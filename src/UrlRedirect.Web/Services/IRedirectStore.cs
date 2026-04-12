using UrlRedirect.Web.Models;

namespace UrlRedirect.Web.Services;

public interface IRedirectStore
{
    Task<bool> TryCreateAsync(RedirectRecord record, CancellationToken cancellationToken);
}
