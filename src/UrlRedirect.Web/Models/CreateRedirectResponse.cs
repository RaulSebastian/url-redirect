namespace UrlRedirect.Web.Models;

public sealed record CreateRedirectResponse(
    string Alias,
    string ShortUrl,
    string TargetUrl,
    int StatusCode);
