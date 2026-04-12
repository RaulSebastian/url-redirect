using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using UrlRedirect.Web.Models;
using UrlRedirect.Web.Services;

namespace UrlRedirect.Web.Pages;

public sealed class IndexModel : PageModel
{
    private readonly IRedirectStore _redirectStore;

    public IndexModel(IRedirectStore redirectStore)
    {
        _redirectStore = redirectStore;
    }

    [BindProperty]
    public CreateRedirectInput Input { get; set; } = new();

    public string? CreatedRedirectUrl { get; private set; }

    public void OnGet()
    {
    }

    public async Task OnPostAsync(CancellationToken cancellationToken)
    {
        var validationErrors = CreateRedirectRequestValidator.Validate(Input);

        foreach (var validationError in validationErrors)
        {
            var modelKey = string.IsNullOrEmpty(validationError.Key)
                ? string.Empty
                : $"Input.{validationError.Key}";

            foreach (var message in validationError.Value)
            {
                ModelState.AddModelError(modelKey, message);
            }
        }

        if (!ModelState.IsValid)
        {
            return;
        }

        var created = await _redirectStore.TryCreateAsync(
            new RedirectRecord(
                Input.Alias,
                Input.TargetUrl,
                DateTime.UtcNow),
            cancellationToken);

        if (!created)
        {
            ModelState.AddModelError("Input.Alias", "That alias is already in use.");
            return;
        }

        var host = $"{Request.Scheme}://{Request.Host}";
        CreatedRedirectUrl = $"{host}/{Input.Alias}";
    }
}
