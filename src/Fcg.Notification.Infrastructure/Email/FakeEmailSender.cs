using Fcg.Notification.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fcg.Notification.Infrastructure.Email;

/// <summary>Implementação fake: apenas loga o envio. Útil para testes e desenvolvimento local.</summary>
public sealed class FakeEmailSender : IEmailSender
{
    private readonly ILogger<FakeEmailSender> _logger;

    public FakeEmailSender(ILogger<FakeEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string to, string subject, string htmlBody, string? textBody, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("FakeEmailSender: To={To}, Subject={Subject}, BodyLength={Length}", to, subject, htmlBody?.Length ?? 0);
        return Task.CompletedTask;
    }
}
