namespace Fcg.Notification.Application.Abstractions;

/// <summary>Resolve conteúdo do template por nome (e opcionalmente versão/idioma). Retorna null se inexistente.</summary>
public interface ITemplateResolver
{
    Task<string?> ResolveAsync(string templateName, string? language = null, CancellationToken cancellationToken = default);
}
