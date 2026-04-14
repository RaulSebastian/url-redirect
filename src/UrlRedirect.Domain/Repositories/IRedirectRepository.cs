using UrlRedirect.Domain.Model;

namespace UrlRedirect.Domain.Repositories;

public interface IRedirectRepository
{
    Task<bool> TryCreateAsync(Redirect redirect, CancellationToken cancellationToken);
    Task<Redirect?> GetByAliasAsync(string alias, CancellationToken cancellationToken);
    Task<IReadOnlyList<Redirect>> GetAllAsync(CancellationToken cancellationToken);
    Task<bool> DeleteAsync(string alias, CancellationToken cancellationToken);
}
