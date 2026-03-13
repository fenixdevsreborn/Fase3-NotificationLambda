namespace Fcg.Notification.Application.Abstractions;

/// <summary>Renderiza um template com um payload (objeto) e retorna HTML/texto final.</summary>
public interface ITemplateRenderer
{
    string Render(string templateContent, object payload);
}
