namespace UrlRedirect.Web.Models;

public sealed record RedirectRecord(
    string Alias,
    string TargetUrl,
    DateTime CreatedUtc);
