using System.Reflection;
using System.Text;
using Fcg.Notification.Application.Abstractions;

namespace Fcg.Notification.Infrastructure.Templates;

/// <summary>Resolve templates por nome a partir de recursos embarcados (Templates.{TemplateName}.html) ou pasta opcional.</summary>
public sealed class TemplateResolver : ITemplateResolver
{
    private readonly string? _templatesPath;
    private readonly Assembly _assembly;
    private readonly string _resourcePrefix;

    public TemplateResolver(string? templatesPath = null)
    {
        _templatesPath = templatesPath;
        _assembly = Assembly.GetExecutingAssembly();
        _resourcePrefix = $"{typeof(TemplateResolver).Namespace}.";
    }

    public async Task<string?> ResolveAsync(string templateName, string? language = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(templateName)) return null;

        var normalizedName = templateName.Replace("/", ".", StringComparison.Ordinal);
        var resourceName = $"{_resourcePrefix}{normalizedName}.html";
        var stream = _assembly.GetManifestResourceStream(resourceName);
        if (stream is not null)
        {
            using var reader = new StreamReader(stream, Encoding.UTF8);
            return await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        }

        if (!string.IsNullOrWhiteSpace(_templatesPath))
        {
            var path = Path.Combine(_templatesPath, $"{normalizedName}.html");
            if (File.Exists(path))
                return await File.ReadAllTextAsync(path, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
        }

        return null;
    }
}
