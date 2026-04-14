using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net.Sockets;
using UrlRedirect.Domain.Model;
using UrlRedirect.Infrastructure.Repositories;
using Xunit;

namespace UrlRedirect.Web.Tests;

public sealed class AzureTableRedirectRepositoryIntegrationTests
{
    [SkippableFact]
    public async Task TryCreateAndLookup_UsesRealTableStorage()
    {
        Skip.IfNot(await AzureTableRepositoryTestContext.IsAzuriteAvailableAsync(), AzureTableRepositoryTestContext.SkipMessage);
        await using var context = await AzureTableRepositoryTestContext.CreateAsync();

        var created = await context.Repository.TryCreateAsync(
            new Redirect("summer-sale", "https://example.com/campaign", DateTime.UtcNow),
            CancellationToken.None);

        var redirect = await context.Repository.GetByAliasAsync("summer-sale", CancellationToken.None);

        Assert.True(created);
        Assert.NotNull(redirect);
        Assert.Equal("summer-sale", redirect.Alias);
        Assert.Equal("https://example.com/campaign", redirect.TargetUrl);
    }

    [SkippableFact]
    public async Task TryCreate_WhenAliasAlreadyExists_ReturnsFalse()
    {
        Skip.IfNot(await AzureTableRepositoryTestContext.IsAzuriteAvailableAsync(), AzureTableRepositoryTestContext.SkipMessage);
        await using var context = await AzureTableRepositoryTestContext.CreateAsync();

        await context.Repository.TryCreateAsync(
            new Redirect("summer-sale", "https://example.com/campaign", DateTime.UtcNow),
            CancellationToken.None);

        var created = await context.Repository.TryCreateAsync(
            new Redirect("summer-sale", "https://example.com/other", DateTime.UtcNow),
            CancellationToken.None);

        Assert.False(created);
    }

    [SkippableFact]
    public async Task GetByAlias_WhenAliasDoesNotExist_ReturnsNull()
    {
        Skip.IfNot(await AzureTableRepositoryTestContext.IsAzuriteAvailableAsync(), AzureTableRepositoryTestContext.SkipMessage);
        await using var context = await AzureTableRepositoryTestContext.CreateAsync();

        var redirect = await context.Repository.GetByAliasAsync("missing-alias", CancellationToken.None);

        Assert.Null(redirect);
    }

    [SkippableFact]
    public async Task GetAllAndDelete_WorkAgainstRealTableStorage()
    {
        Skip.IfNot(await AzureTableRepositoryTestContext.IsAzuriteAvailableAsync(), AzureTableRepositoryTestContext.SkipMessage);
        await using var context = await AzureTableRepositoryTestContext.CreateAsync();

        await context.Repository.TryCreateAsync(
            new Redirect("alpha", "https://example.com/a", DateTime.UtcNow.AddMinutes(-2)),
            CancellationToken.None);
        await context.Repository.TryCreateAsync(
            new Redirect("beta", "https://example.com/b", DateTime.UtcNow.AddMinutes(-1)),
            CancellationToken.None);

        var redirects = await context.Repository.GetAllAsync(CancellationToken.None);
        var deleted = await context.Repository.DeleteAsync("alpha", CancellationToken.None);
        var deletedRedirect = await context.Repository.GetByAliasAsync("alpha", CancellationToken.None);

        Assert.Equal(2, redirects.Count);
        Assert.True(deleted);
        Assert.Null(deletedRedirect);
    }

    private sealed class AzureTableRepositoryTestContext : IAsyncDisposable
    {
        private const string ConnectionString = "UseDevelopmentStorage=true";
        public const string SkipMessage = "Azurite is required for Azure Table integration tests. Start it with 'docker compose -f compose.azurite.yml up -d'.";

        private AzureTableRepositoryTestContext(TableClient tableClient)
        {
            TableClient = tableClient;
            Repository = new AzureTableRedirectRepository(
                tableClient,
                NullLogger<AzureTableRedirectRepository>.Instance);
        }

        public TableClient TableClient { get; }

        public AzureTableRedirectRepository Repository { get; }

        public static async Task<AzureTableRepositoryTestContext> CreateAsync()
        {
            var tableName = $"redirectsit{Guid.NewGuid():N}";
            var tableClient = new TableClient(ConnectionString, tableName);

            await tableClient.CreateIfNotExistsAsync();

            return new AzureTableRepositoryTestContext(tableClient);
        }

        public static async Task<bool> IsAzuriteAvailableAsync()
        {
            try
            {
                using var client = new TcpClient();
                using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(1));
                await client.ConnectAsync("127.0.0.1", 10002, cancellationSource.Token);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                await TableClient.DeleteAsync();
            }
            catch
            {
                // Best-effort cleanup for emulator tables.
            }
        }

    }
}
