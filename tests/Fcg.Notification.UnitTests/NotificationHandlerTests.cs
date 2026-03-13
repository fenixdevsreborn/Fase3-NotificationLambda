using Fcg.Notification.Application.Abstractions;
using Fcg.Notification.Application.Services;
using Fcg.Notification.Contracts;
using Fcg.Notification.Contracts.Messages;
using Fcg.Notification.Contracts.Payloads;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Fcg.Notification.UnitTests;

public class NotificationHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenMessageIdEmpty_ReturnsValidationFailed()
    {
        var sut = CreateSut();
        var msg = new NotificationMessage { MessageId = "", TemplateName = TemplateNames.PaymentApproved };
        var result = await sut.HandleAsync(msg);
        Assert.Equal(NotificationResultStatus.ValidationFailed, result.Status);
        Assert.Equal("MessageId is required", result.ErrorMessage);
    }

    [Fact]
    public async Task HandleAsync_WhenAlreadySent_ReturnsSkippedDuplicate()
    {
        var sentStore = new FakeSentStore(alreadySent: true);
        var sut = CreateSut(sentStore: sentStore);
        var msg = new NotificationMessage { MessageId = "m1", TemplateName = TemplateNames.PaymentApproved };
        var result = await sut.HandleAsync(msg);
        Assert.Equal(NotificationResultStatus.SkippedDuplicate, result.Status);
    }

    [Fact]
    public async Task HandleAsync_WhenDeserializerReturnsNull_ReturnsValidationFailed()
    {
        var deserializer = new FakeDeserializer(null);
        var sut = CreateSut(deserializer: deserializer);
        var msg = new NotificationMessage { MessageId = "m1", PayloadType = "Unknown", Payload = "{}" };
        var result = await sut.HandleAsync(msg);
        Assert.Equal(NotificationResultStatus.ValidationFailed, result.Status);
    }

    [Fact]
    public async Task HandleAsync_WhenValidationFails_ReturnsValidationFailed()
    {
        var payload = new PaymentApprovedEmailPayload { PaymentId = Guid.NewGuid(), UserEmail = "" };
        var deserializer = new FakeDeserializer(payload);
        var validator = new FakeValidator(new List<string> { "UserEmail is required" });
        var sut = CreateSut(deserializer: deserializer, validator: validator);
        var msg = new NotificationMessage { MessageId = "m1", TemplateName = TemplateNames.PaymentApproved, Payload = "{}" };
        var result = await sut.HandleAsync(msg);
        Assert.Equal(NotificationResultStatus.ValidationFailed, result.Status);
        Assert.NotNull(result.ValidationErrors);
        Assert.Single(result.ValidationErrors);
    }

    [Fact]
    public async Task HandleAsync_WhenTemplateNotFound_ReturnsTemplateNotFound()
    {
        var payload = new PaymentApprovedEmailPayload { PaymentId = Guid.NewGuid(), UserEmail = "a@b.com" };
        var deserializer = new FakeDeserializer(payload);
        var resolver = new FakeResolver(null);
        var sut = CreateSut(deserializer: deserializer, resolver: resolver);
        var msg = new NotificationMessage { MessageId = "m1", TemplateName = "Missing", Payload = "{}" };
        var result = await sut.HandleAsync(msg);
        Assert.Equal(NotificationResultStatus.TemplateNotFound, result.Status);
    }

    [Fact]
    public async Task HandleAsync_WhenAllOk_MarksSentAndReturnsSent()
    {
        var payload = new PaymentApprovedEmailPayload { PaymentId = Guid.NewGuid(), UserEmail = "u@e.com", UserName = "User" };
        var sentStore = new FakeSentStore(alreadySent: false);
        var deserializer = new FakeDeserializer(payload);
        var resolver = new FakeResolver("<html>{{playerName}}</html>");
        var modelBuilder = new FakeModelBuilder(new { playerName = "User" });
        var renderer = new FakeRenderer("<html>User</html>");
        var emailSender = new FakeEmailSender();
        var sut = CreateSut(sentStore: sentStore, deserializer: deserializer, resolver: resolver, modelBuilder: modelBuilder, renderer: renderer, emailSender: emailSender);
        var msg = new NotificationMessage { MessageId = "m1", TemplateName = TemplateNames.PaymentApproved, Payload = "{}" };
        var result = await sut.HandleAsync(msg);
        Assert.Equal(NotificationResultStatus.Sent, result.Status);
        Assert.True(sentStore.MarkSentCalled);
    }

    private static NotificationHandler CreateSut(
        ISentNotificationStore? sentStore = null,
        IPayloadDeserializer? deserializer = null,
        IPayloadValidator? validator = null,
        ITemplateResolver? resolver = null,
        ITemplateModelBuilder? modelBuilder = null,
        ITemplateRenderer? renderer = null,
        IEmailSender? emailSender = null)
    {
        sentStore ??= new FakeSentStore(false);
        deserializer ??= new FakeDeserializer(null);
        validator ??= new FakeValidator(null);
        resolver ??= new FakeResolver("");
        modelBuilder ??= new FakeModelBuilder(null);
        renderer ??= new FakeRenderer("");
        emailSender ??= new FakeEmailSender();
        return new NotificationHandler(resolver, renderer, modelBuilder, deserializer, validator, emailSender, sentStore, NullLogger<NotificationHandler>.Instance);
    }

    private sealed class FakeSentStore : ISentNotificationStore
    {
        private readonly bool _alreadySent;
        public bool MarkSentCalled { get; private set; }
        public FakeSentStore(bool alreadySent) => _alreadySent = alreadySent;
        public Task<bool> WasSentAsync(string messageId, CancellationToken ct) => Task.FromResult(_alreadySent);
        public Task<bool> TryMarkSentAsync(string messageId, CancellationToken ct) { MarkSentCalled = true; return Task.FromResult(true); }
    }

    private sealed class FakeDeserializer : IPayloadDeserializer
    {
        private readonly object? _result;
        public FakeDeserializer(object? result) => _result = result;
        public object? Deserialize(NotificationMessage message) => _result;
    }

    private sealed class FakeValidator : IPayloadValidator
    {
        private readonly IReadOnlyList<string>? _errors;
        public FakeValidator(IReadOnlyList<string>? errors) => _errors = errors;
        public IReadOnlyList<string>? Validate(NotificationMessage message, object payload) => _errors;
    }

    private sealed class FakeResolver : ITemplateResolver
    {
        private readonly string? _content;
        public FakeResolver(string? content) => _content = content;
        public Task<string?> ResolveAsync(string templateName, string? language, CancellationToken ct) => Task.FromResult(_content);
    }

    private sealed class FakeModelBuilder : ITemplateModelBuilder
    {
        private readonly object? _model;
        public FakeModelBuilder(object? model) => _model = model;
        public object? BuildModel(string templateName, object payload) => _model ?? payload;
    }

    private sealed class FakeRenderer : ITemplateRenderer
    {
        private readonly string _result;
        public FakeRenderer(string result) => _result = result;
        public string Render(string templateContent, object payload) => _result;
    }

    private sealed class FakeEmailSender : IEmailSender
    {
        public Task SendAsync(string to, string subject, string htmlBody, string? textBody, CancellationToken ct) => Task.CompletedTask;
    }
}
