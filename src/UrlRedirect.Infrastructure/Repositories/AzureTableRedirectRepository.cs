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
}
