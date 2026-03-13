namespace Fcg.Notification.Infrastructure.Options;

public sealed class NotificationOptions
{
    public const string SectionName = "Notification";
    public string LibraryBaseUrl { get; set; } = "https://fcg.com/library";
    public string SupportUrl { get; set; } = "https://fcg.com/support";
}
