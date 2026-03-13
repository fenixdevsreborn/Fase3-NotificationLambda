namespace Fcg.Notification.Contracts.Payloads;

/// <summary>Base para payloads de e-mail de pagamento; contém o suficiente para renderizar templates.</summary>
public abstract class PaymentEmailPayloadBase
{
    public Guid UserId { get; set; }
    public string? UserName { get; set; }
    public string? UserEmail { get; set; }
    public Guid GameId { get; set; }
    public string? GameTitle { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "BRL";
    public Guid PaymentId { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public DateTime PurchaseDate { get; set; }
}
