using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using Fcg.Notification.Application.Abstractions;

namespace Fcg.Notification.Infrastructure.Templates;

/// <summary>Substitui {{PropertyName}} pelo valor da propriedade do payload (case-insensitive).</summary>
public sealed class SimpleTemplateRenderer : ITemplateRenderer
{
    private static readonly Regex PlaceholderRegex = new(@"\{\{(\w+)\}\}", RegexOptions.Compiled);

    public string Render(string templateContent, object payload)
    {
        if (string.IsNullOrEmpty(templateContent)) return string.Empty;
        var dict = ToDictionary(payload);
        return PlaceholderRegex.Replace(templateContent, m =>
        {
            var key = m.Groups[1].Value;
            if (dict.TryGetValue(key, out var value) && value is not null)
                return value.ToString() ?? string.Empty;
            return m.Value;
        });
    }

    private static Dictionary<string, object?> ToDictionary(object obj)
    {
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        var type = obj.GetType();
        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!prop.CanRead) continue;
            var value = prop.GetValue(obj);
            if (value is DateTime dt)
                result[prop.Name] = dt.ToString("O", CultureInfo.InvariantCulture);
            else
                result[prop.Name] = value;
        }
        return result;
    }
}
