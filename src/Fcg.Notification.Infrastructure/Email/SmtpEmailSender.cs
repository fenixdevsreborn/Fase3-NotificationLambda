using System.Net;
using System.Net.Mail;
using Fcg.Notification.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fcg.Notification.Infrastructure.Email;

public sealed class SmtpEmailSender : IEmailSender
{
    private readonly SmtpOptions _options;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<SmtpOptions> options, ILogger<SmtpEmailSender> logger)
    {
        _options = options?.Value ?? new SmtpOptions();
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string htmlBody, string? textBody, CancellationToken cancellationToken = default)
    {
        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.EnableSsl,
            Credentials = string.IsNullOrEmpty(_options.UserName) ? null : new NetworkCredential(_options.UserName, _options.Password)
        };
        var from = string.IsNullOrEmpty(_options.FromAddress) ? "noreply@fcg.local" : _options.FromAddress;
        using var msg = new MailMessage(from, to, subject, textBody ?? htmlBody)
        {
            IsBodyHtml = !string.IsNullOrEmpty(htmlBody) && htmlBody != textBody
        };
        if (msg.IsBodyHtml)
            msg.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(htmlBody, null, "text/html"));
        await client.SendMailAsync(msg, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Email sent to {To}, Subject={Subject}", to, subject);
    }
}

public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 25;
    public bool EnableSsl { get; set; }
    public string? UserName { get; set; }
    public string? Password { get; set; }
    public string? FromAddress { get; set; }
}
