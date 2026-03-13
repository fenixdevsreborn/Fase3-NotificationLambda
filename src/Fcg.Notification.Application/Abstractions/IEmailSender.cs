namespace Fcg.Notification.Application.Abstractions;

/// <summary>Abstração do envio de e-mail. Implementações: SMTP, fake, SES.</summary>
public interface IEmailSender
{
    Task SendAsync(string to, string subject, string htmlBody, string? textBody, CancellationToken cancellationToken = default);
}
