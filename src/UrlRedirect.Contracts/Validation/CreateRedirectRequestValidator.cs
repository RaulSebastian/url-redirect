using UrlRedirect.Contracts.Models;
using UrlRedirect.Domain.Validation;

namespace UrlRedirect.Contracts.Validation;

public static class CreateRedirectRequestValidator
{
    public static IReadOnlyDictionary<string, string[]> Validate(CreateRedirectRequest request)
    {
        var alias = RedirectRequestValidator.NormalizeAlias(request.Alias ?? string.Empty);
        var targetUrl = RedirectRequestValidator.NormalizeTargetUrl(request.TargetUrl ?? string.Empty);

        request.Alias = alias;
        request.TargetUrl = targetUrl;

        return RedirectRequestValidator.Validate(alias, targetUrl);
    }
}
