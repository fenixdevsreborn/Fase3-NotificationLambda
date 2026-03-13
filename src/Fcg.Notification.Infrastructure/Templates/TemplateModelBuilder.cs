using System.Globalization;
using Fcg.Notification.Application.Abstractions;
using Fcg.Notification.Contracts;
using Fcg.Notification.Contracts.Payloads;
using Fcg.Notification.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace Fcg.Notification.Infrastructure.Templates;

/// <summary>Mapeia payloads fortemente tipados para o modelo esperado pelos templates HTML (playerName, gameTitle, orderId, etc.).</summary>
public sealed class TemplateModelBuilder : ITemplateModelBuilder
{
    private readonly NotificationOptions _options;
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    public TemplateModelBuilder(IOptions<NotificationOptions> options)
    {
        _options = options?.Value ?? new NotificationOptions();
    }

    public object? BuildModel(string templateName, object payload)
    {
        if (payload is PaymentApprovedEmailPayload approved && templateName == TemplateNames.PaymentApproved)
            return BuildPaymentApprovedModel(approved);
        if (payload is PaymentFailedEmailPayload failed && templateName == TemplateNames.PaymentFailed)
            return BuildPaymentFailedModel(failed);
        return payload;
    }

    private object BuildPaymentApprovedModel(PaymentApprovedEmailPayload p)
    {
        return new
        {
            playerName = p.UserName ?? "Jogador",
            gameTitle = p.GameTitle ?? "Jogo",
            orderId = p.PaymentId.ToString(),
            purchaseDate = p.PurchaseDate.ToString("dd/MM/yyyy 'às' HH:mm", PtBr),
            totalAmount = FormatMoney(p.Amount, p.Currency),
            libraryUrl = _options.LibraryBaseUrl.TrimEnd('/')
        };
    }

    private object BuildPaymentFailedModel(PaymentFailedEmailPayload p)
    {
        return new
        {
            playerName = p.UserName ?? "Jogador",
            gameTitle = p.GameTitle ?? "Jogo",
            orderId = p.PaymentId.ToString(),
            purchaseDate = p.PurchaseDate.ToString("dd/MM/yyyy 'às' HH:mm", PtBr),
            totalAmount = FormatMoney(p.Amount, p.Currency),
            failureReason = p.FailureReason ?? "Não foi possível processar o pagamento.",
            supportUrl = _options.SupportUrl.TrimEnd('/')
        };
    }

    private static string FormatMoney(decimal amount, string currency)
    {
        if (string.Equals(currency, "BRL", StringComparison.OrdinalIgnoreCase))
            return amount.ToString("C", PtBr);
        return $"{currency} {amount:N2}";
    }
}
