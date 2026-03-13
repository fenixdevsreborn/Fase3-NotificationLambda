namespace Fcg.Notification.Application.Abstractions;

/// <summary>Constrói o modelo (objeto) usado na renderização do template a partir do payload. Mapeia nomes do contrato para placeholders do HTML (ex.: UserName → playerName).</summary>
public interface ITemplateModelBuilder
{
    object? BuildModel(string templateName, object payload);
}
