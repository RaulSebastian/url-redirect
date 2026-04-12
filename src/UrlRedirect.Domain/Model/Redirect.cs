namespace UrlRedirect.Domain.Model;

public sealed record Redirect(
    string Alias,
    string TargetUrl,
    DateTime CreatedUtc);
