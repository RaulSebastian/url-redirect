using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace UrlRedirect.Web.Pages.Admin;

public sealed class IndexModel : PageModel
{
    public string DisplayName { get; private set; } = "admin";

    public void OnGet()
    {
        DisplayName =
            User.FindFirstValue("name") ??
            User.FindFirstValue(ClaimTypes.Name) ??
            User.FindFirstValue("preferred_username") ??
            User.Identity?.Name ??
            "admin";
    }
}
