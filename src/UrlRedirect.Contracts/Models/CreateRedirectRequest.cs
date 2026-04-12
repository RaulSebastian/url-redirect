namespace UrlRedirect.Contracts.Models;

public sealed record CreateRedirectRequest
{
    public string? Alias { get; set; } = string.Empty;

    public string? TargetUrl { get; set; } = string.Empty;
}
