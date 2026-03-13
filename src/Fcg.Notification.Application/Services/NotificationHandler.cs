using Fcg.Notification.Application.Abstractions;
using Fcg.Notification.Contracts;
using Fcg.Notification.Contracts.Messages;
using Fcg.Notification.Contracts.Payloads;
using Microsoft.Extensions.Logging;

namespace Fcg.Notification.Application.Services;

public sealed class NotificationHandler : INotificationHandler
{
    private readonly ITemplateResolver _templateResolver;
    private readonly ITemplateRenderer _renderer;
    private readonly ITemplateModelBuilder _modelBuilder;
    private readonly IPayloadDeserializer _deserializer;
    private readonly IPayloadValidator _validator;
    private readonly IEmailSender _emailSender;
    private readonly ISentNotificationStore _sentStore;
    private readonly ILogger<NotificationHandler> _logger;

    public NotificationHandler(
        ITemplateResolver templateResolver,
        ITemplateRenderer renderer,
        ITemplateModelBuilder modelBuilder,
        IPayloadDeserializer deserializer,
        IPayloadValidator validator,
        IEmailSender emailSender,
        ISentNotificationStore sentStore,
        ILogger<NotificationHandler> logger)
    {
        _templateResolver = templateResolver;
        _renderer = renderer;
        _modelBuilder = modelBuilder;
        _deserializer = deserializer;
        _validator = validator;
        _emailSender = emailSender;
        _sentStore = sentStore;
        _logger = logger;
    }

    public async Task<NotificationResult> HandleAsync(NotificationMessage message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(message.MessageId))
        {
            _logger.LogWarning("Message has no MessageId");
            return new NotificationResult { Status = NotificationResultStatus.ValidationFailed, ErrorMessage = "MessageId is required" };
        }

        if (await _sentStore.WasSentAsync(message.MessageId, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogInformation("Message {MessageId} already sent; skipping (idempotent)", message.MessageId);
            return new NotificationResult { Status = NotificationResultStatus.SkippedDuplicate };
        }

        var payload = _deserializer.Deserialize(message);
        if (payload is null)
        {
            _logger.LogWarning("Unknown or invalid PayloadType {PayloadType} for MessageId {MessageId}", message.PayloadType, message.MessageId);
            return new NotificationResult { Status = NotificationResultStatus.ValidationFailed, ErrorMessage = "Unknown PayloadType or invalid JSON" };
        }

        var validationErrors = _validator.Validate(message, payload);
        if (validationErrors is { Count: > 0 })
        {
            _logger.LogWarning("Validation failed for MessageId {MessageId}: {Errors}", message.MessageId, string.Join("; ", validationErrors));
            return new NotificationResult { Status = NotificationResultStatus.ValidationFailed, ValidationErrors = validationErrors };
        }

        var templateContent = await _templateResolver.ResolveAsync(message.TemplateName, message.Language, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(templateContent))
        {
            _logger.LogWarning("Template not found: {TemplateName} for MessageId {MessageId}", message.TemplateName, message.MessageId);
            return new NotificationResult { Status = NotificationResultStatus.TemplateNotFound, ErrorMessage = $"Template '{message.TemplateName}' not found" };
        }

        var templateModel = _modelBuilder.BuildModel(message.TemplateName, payload) ?? payload;
        string htmlBody;
        try
        {
            htmlBody = _renderer.Render(templateContent, templateModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Template render failed for MessageId {MessageId}", message.MessageId);
            return new NotificationResult { Status = NotificationResultStatus.SendFailed, ErrorMessage = ex.Message };
        }

        var recipient = !string.IsNullOrWhiteSpace(message.Recipient) ? message.Recipient : GetRecipientFromPayload(payload);
        if (string.IsNullOrWhiteSpace(recipient))
        {
            _logger.LogWarning("No recipient for MessageId {MessageId}", message.MessageId);
            return new NotificationResult { Status = NotificationResultStatus.ValidationFailed, ErrorMessage = "Recipient is required" };
        }

        var subject = BuildSubject(message.TemplateName, payload);
        try
        {
            await _emailSender.SendAsync(recipient, subject, htmlBody, null, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Send failed for MessageId {MessageId}", message.MessageId);
            return new NotificationResult { Status = NotificationResultStatus.SendFailed, ErrorMessage = ex.Message };
        }

        await _sentStore.TryMarkSentAsync(message.MessageId, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Notification sent for MessageId {MessageId}, Template {TemplateName}", message.MessageId, message.TemplateName);
        return new NotificationResult { Status = NotificationResultStatus.Sent };
    }

    private static string? GetRecipientFromPayload(object payload)
    {
        if (payload is PaymentEmailPayloadBase payment)
            return payment.UserEmail;
        return null;
    }

    private static string BuildSubject(string templateName, object payload)
    {
        if (payload is PaymentEmailPayloadBase payment)
        {
            return templateName switch
            {
                TemplateNames.PaymentApproved => $"Pagamento aprovado - {payment.GameTitle ?? "Compra"}",
                TemplateNames.PaymentFailed => $"Pagamento não concluído - {payment.GameTitle ?? "Compra"}",
                _ => "Notificação FCG"
            };
        }
        return "Notificação FCG";
    }
}
