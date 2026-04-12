using UrlRedirect.Contracts.Models;
using UrlRedirect.Domain.Validation;

namespace UrlRedirect.Contracts.Validation;

public static class CreateRedirectRequestValidator
{
    public static IReadOnlyDictionary<string, string[]> Validate(CreateRedirectRequest request)
    {
        request.Alias = RedirectRequestValidator.NormalizeAlias(request.Alias);
        request.TargetUrl = RedirectRequestValidator.NormalizeTargetUrl(request.TargetUrl);

        return RedirectRequestValidator.Validate(request.Alias, request.TargetUrl);
    }
}
