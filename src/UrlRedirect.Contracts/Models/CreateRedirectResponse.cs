namespace UrlRedirect.Contracts.Models;

public sealed record CreateRedirectResponse(
    string Alias,
    string ShortUrl,
    string TargetUrl,
    int StatusCode);
