using Azure.Data.Tables;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using UrlRedirect.Domain.Repositories;
using UrlRedirect.Infrastructure.Repositories;

namespace UrlRedirect.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRedirectInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddSingleton(serviceProvider =>
        {
            var connectionString = configuration.GetConnectionString("RedirectStorage")
                ?? configuration["RedirectStorage:ConnectionString"]
                ?? configuration["AzureWebJobsStorage"]
                ?? throw new InvalidOperationException(
                    "Redirect storage connection string is not configured. Set ConnectionStrings:RedirectStorage, RedirectStorage:ConnectionString, or AzureWebJobsStorage.");

            var tableName = configuration["RedirectStorage:TableName"] ?? "redirects";
            var tableClient = new TableClient(connectionString, tableName);
            tableClient.CreateIfNotExists();
            return tableClient;
        });

        services.AddSingleton<IRedirectRepository, AzureTableRedirectRepository>();
        services.AddHealthChecks()
            .AddCheck<RedirectStorageHealthCheck>("redirect-storage");
        return services;
    }
}
