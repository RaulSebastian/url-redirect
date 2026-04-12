using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using UrlRedirect.Web.Models;

namespace UrlRedirect.Web.Pages;

public sealed class IndexModel : PageModel
{
    [BindProperty]
    public CreateRedirectInput Input { get; set; } = new();

    public string? CreatedRedirectUrl { get; private set; }

    public void OnGet()
    {
    }

    public void OnPost()
    {
        Input.Alias = Input.Alias.Trim().ToLowerInvariant();
        Input.TargetUrl = Input.TargetUrl.Trim();

        if (!Uri.TryCreate(Input.TargetUrl, UriKind.Absolute, out _))
        {
            ModelState.AddModelError("Input.TargetUrl", "Enter a valid absolute URL.");
        }

        if (!ModelState.IsValid)
        {
            return;
        }

        var host = $"{Request.Scheme}://{Request.Host}";
        CreatedRedirectUrl = $"{host}/{Input.Alias}";
    }
}
