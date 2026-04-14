using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace UrlRedirect.Infrastructure;

public sealed class RedirectStorageHealthCheck : IHealthCheck
{
    private readonly TableClient _tableClient;

    public RedirectStorageHealthCheck(TableClient tableClient)
    {
        _tableClient = tableClient;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _tableClient.GetEntityIfExistsAsync<TableEntity>(
                "__health__",
                "__health__",
                cancellationToken: cancellationToken);

            return HealthCheckResult.Healthy("Redirect storage is reachable.");
        }
        catch (RequestFailedException exception)
        {
            return HealthCheckResult.Unhealthy("Redirect storage is unavailable.", exception);
        }
    }
}
