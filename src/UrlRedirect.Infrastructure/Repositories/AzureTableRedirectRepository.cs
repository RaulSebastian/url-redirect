using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using UrlRedirect.Domain.Model;
using UrlRedirect.Domain.Repositories;

namespace UrlRedirect.Infrastructure.Repositories;

public sealed class AzureTableRedirectRepository : IRedirectRepository
{
    private const string PartitionKey = "redirect";
    private readonly TableClient _tableClient;
    private readonly ILogger<AzureTableRedirectRepository> _logger;

    public AzureTableRedirectRepository(
        TableClient tableClient,
        ILogger<AzureTableRedirectRepository> logger)
    {
        _tableClient = tableClient;
        _logger = logger;
    }

    public async Task<bool> TryCreateAsync(Redirect redirect, CancellationToken cancellationToken)
    {
        var entity = new TableRedirectEntity
        {
            PartitionKey = PartitionKey,
            RowKey = redirect.Alias,
            TargetUrl = redirect.TargetUrl,
            CreatedUtc = redirect.CreatedUtc
        };

        try
        {
            await _tableClient.AddEntityAsync(entity, cancellationToken);
            return true;
        }
        catch (RequestFailedException exception) when (exception.Status == 409)
        {
            _logger.LogInformation("Duplicate redirect alias rejected: {Alias}", redirect.Alias);
            return false;
        }
    }

    public async Task<Redirect?> GetByAliasAsync(string alias, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _tableClient.GetEntityAsync<TableRedirectEntity>(
                PartitionKey,
                alias,
                cancellationToken: cancellationToken);

            return new Redirect(
                response.Value.RowKey,
                response.Value.TargetUrl,
                response.Value.CreatedUtc);
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<Redirect>> GetAllAsync(CancellationToken cancellationToken)
    {
        var redirects = new List<Redirect>();

        await foreach (var entity in _tableClient.QueryAsync<TableRedirectEntity>(
                           entity => entity.PartitionKey == PartitionKey,
                           cancellationToken: cancellationToken))
        {
            redirects.Add(new Redirect(
                entity.RowKey,
                entity.TargetUrl,
                entity.CreatedUtc));
        }

        return redirects
            .OrderByDescending(static redirect => redirect.CreatedUtc)
            .ThenBy(static redirect => redirect.Alias, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public async Task<bool> DeleteAsync(string alias, CancellationToken cancellationToken)
    {
        try
        {
            await _tableClient.DeleteEntityAsync(
                PartitionKey,
                alias,
                ETag.All,
                cancellationToken);

            return true;
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            return false;
        }
    }
}
