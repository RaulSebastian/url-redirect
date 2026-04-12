using System.ComponentModel.DataAnnotations;

namespace UrlRedirect.Web.Models;

public sealed class CreateRedirectInput
{
    private const string AliasPattern = "^[a-z0-9][a-z0-9-_]{2,39}$";

    [Required]
    [Display(Name = "Custom alias")]
    [RegularExpression(
        AliasPattern,
        ErrorMessage = "Use 3 to 40 lowercase characters with letters, numbers, hyphens, or underscores.")]
    public string Alias { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Target URL")]
    [Url(ErrorMessage = "Enter a valid absolute URL.")]
    public string TargetUrl { get; set; } = string.Empty;
}
