using Azure.Data.Tables;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
        if (environment.IsDevelopment())
        {
            services.AddSingleton<IRedirectRepository, JsonRedirectStore>();
            return services;
        }

        services.AddSingleton(serviceProvider =>
        {
            var connectionString = configuration.GetConnectionString("RedirectStorage")
                ?? configuration["RedirectStorage:ConnectionString"]
                ?? throw new InvalidOperationException(
                    "Redirect storage connection string is not configured. Set ConnectionStrings:RedirectStorage or RedirectStorage:ConnectionString.");

            var tableName = configuration["RedirectStorage:TableName"] ?? "redirects";
            var tableClient = new TableClient(connectionString, tableName);
            tableClient.CreateIfNotExists();
            return tableClient;
        });

        services.AddSingleton<IRedirectRepository, AzureTableRedirectRepository>();
        return services;
    }
}
