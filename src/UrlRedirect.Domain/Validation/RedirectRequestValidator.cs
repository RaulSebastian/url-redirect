using System.Text.RegularExpressions;

namespace UrlRedirect.Domain.Validation;

public static partial class RedirectRequestValidator
{
    public static IReadOnlyDictionary<string, string[]> Validate(string alias, string targetUrl)
    {
        var normalizedAlias = NormalizeAlias(alias);
        var normalizedTargetUrl = NormalizeTargetUrl(targetUrl);
        var errors = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        if (string.IsNullOrWhiteSpace(normalizedAlias))
        {
            AddError("Alias", "The Custom alias field is required.");
        }
        else if (ReservedAliases.Contains(normalizedAlias))
        {
            AddError("Alias", $"\"{normalizedAlias}\" is a reserved alias and cannot be used.");
        }
        else if (!AliasPattern().IsMatch(normalizedAlias))
        {
            AddError("Alias", "Use 3 to 40 lowercase characters with letters, numbers, hyphens, or underscores.");
        }

        if (string.IsNullOrWhiteSpace(normalizedTargetUrl))
        {
            AddError("TargetUrl", "The Target URL field is required.");
        }
        else if (!Uri.TryCreate(normalizedTargetUrl, UriKind.Absolute, out _))
        {
            AddError("TargetUrl", "Enter a valid absolute URL.");
        }

        return errors.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.Distinct().ToArray(),
            StringComparer.Ordinal);

        void AddError(string key, string message)
        {
            if (!errors.TryGetValue(key, out var messages))
            {
                messages = [];
                errors[key] = messages;
            }

            messages.Add(message);
        }
    }

    public static string NormalizeAlias(string? alias) =>
        (alias ?? string.Empty).Trim().ToLowerInvariant();

    public static string NormalizeTargetUrl(string? targetUrl) =>
        (targetUrl ?? string.Empty).Trim();

    private static readonly HashSet<string> ReservedAliases = new(StringComparer.Ordinal)
    {
        "api",
        "ui",
        "admin",
        "login",
        "logout",
    };

    [GeneratedRegex("^[a-z0-9][a-z0-9-_]{2,39}$", RegexOptions.CultureInvariant)]
    private static partial Regex AliasPattern();
}
