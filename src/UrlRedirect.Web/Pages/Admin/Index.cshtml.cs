using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using UrlRedirect.Domain.Repositories;

namespace UrlRedirect.Web.Pages.Admin;

public sealed class IndexModel : PageModel
{
    private static readonly int[] AllowedPageSizes = [10, 20, 50];
    private readonly IRedirectRepository _redirectRepository;

    public IndexModel(IRedirectRepository redirectRepository)
    {
        _redirectRepository = redirectRepository;
    }

    public string DisplayName { get; private set; } = "admin";

    public IReadOnlyList<RedirectRow> Redirects { get; private set; } = [];

    public int CurrentPage { get; private set; } = 1;

    public int PageSize { get; private set; } = 10;

    public int TotalCount { get; private set; }

    public int TotalPages { get; private set; }

    public bool HasPreviousPage => CurrentPage > 1;

    public bool HasNextPage => CurrentPage < TotalPages;

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync([FromQuery(Name = "page")] int? pageNumber, int? pageSize, CancellationToken cancellationToken)
    {
        await LoadAsync(pageNumber, pageSize, cancellationToken);
    }

    public async Task<IActionResult> OnPostDeleteAsync(string alias, [FromForm(Name = "page")] int? pageNumber, int? pageSize, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(alias))
        {
            var deleted = await _redirectRepository.DeleteAsync(alias, cancellationToken);
            StatusMessage = deleted
                ? $"Redirect '{alias}' was deleted."
                : $"Redirect '{alias}' was not found.";
        }

        return RedirectToPage(new
        {
            page = NormalizePage(pageNumber),
            pageSize = NormalizePageSize(pageSize)
        });
    }

    private async Task LoadAsync(int? page, int? pageSize, CancellationToken cancellationToken)
    {
        DisplayName = GetDisplayName();
        PageSize = NormalizePageSize(pageSize);
        var redirects = await _redirectRepository.GetAllAsync(cancellationToken);
        TotalCount = redirects.Count;
        TotalPages = Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
        CurrentPage = Math.Min(NormalizePage(page), TotalPages);
        var skip = (CurrentPage - 1) * PageSize;
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        Redirects = redirects
            .Skip(skip)
            .Take(PageSize)
            .Select(redirect => new RedirectRow(
                redirect.Alias,
                $"{baseUrl}/{redirect.Alias}",
                redirect.TargetUrl,
                redirect.CreatedUtc))
            .ToArray();
    }

    private static int NormalizePage(int? page) =>
        page.GetValueOrDefault(1) < 1 ? 1 : page.GetValueOrDefault(1);

    private static int NormalizePageSize(int? pageSize) =>
        AllowedPageSizes.Contains(pageSize.GetValueOrDefault()) ? pageSize.GetValueOrDefault() : 10;

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
