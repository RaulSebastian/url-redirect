using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using UrlRedirect.Domain.Repositories;

namespace UrlRedirect.Web.Pages.Admin;

public sealed class IndexModel : PageModel
{
    private readonly IRedirectRepository _redirectRepository;

    public IndexModel(IRedirectRepository redirectRepository)
    {
        _redirectRepository = redirectRepository;
    }

    public string DisplayName { get; private set; } = "admin";

    public IReadOnlyList<RedirectRow> Redirects { get; private set; } = [];

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostDeleteAsync(string alias, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(alias))
        {
            var deleted = await _redirectRepository.DeleteAsync(alias, cancellationToken);
            StatusMessage = deleted
                ? $"Redirect '{alias}' was deleted."
                : $"Redirect '{alias}' was not found.";
        }

        return RedirectToPage();
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        DisplayName = GetDisplayName();
        var redirects = await _redirectRepository.GetAllAsync(cancellationToken);
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        Redirects = redirects
            .Select(redirect => new RedirectRow(
                redirect.Alias,
                $"{baseUrl}/{redirect.Alias}",
                redirect.TargetUrl,
                redirect.CreatedUtc))
            .ToArray();
    }

    private string GetDisplayName() =>
        User.FindFirstValue("name") ??
        User.FindFirstValue(ClaimTypes.Name) ??
        User.FindFirstValue("preferred_username") ??
        User.Identity?.Name ??
        "admin";

    public sealed record RedirectRow(
        string Alias,
        string ShortUrl,
        string TargetUrl,
        DateTime CreatedUtc);
    }
