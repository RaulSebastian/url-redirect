using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using UrlRedirect.Domain.Model;
using UrlRedirect.Domain.Repositories;
using Xunit;

namespace UrlRedirect.Web.Tests;

public sealed class RedirectLookupTests : IClassFixture<RedirectApplicationFactory>
{
    private readonly RedirectApplicationFactory _factory;

    public RedirectLookupTests(RedirectApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AliasRoute_RedirectsToStoredTargetUrl()
    {
        using var factory = _factory.WithServices(services =>
        {
            services.RemoveAll<IRedirectRepository>();
            services.AddSingleton<IRedirectRepository>(
                new InMemoryRedirectRepository(
                    new Redirect("summer-sale", "https://example.com/campaign", DateTime.UtcNow)));
        });

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/Summer-Sale");

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal("https://example.com/campaign", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task AliasRoute_ReturnsNotFoundWhenAliasDoesNotExist()
    {
        using var factory = _factory.WithServices(services =>
        {
            services.RemoveAll<IRedirectRepository>();
            services.AddSingleton<IRedirectRepository>(new InMemoryRedirectRepository());
        });

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/missing-alias");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private sealed class InMemoryRedirectRepository : IRedirectRepository
    {
        private readonly Dictionary<string, Redirect> _redirects;

        public InMemoryRedirectRepository(params Redirect[] redirects)
        {
            _redirects = redirects.ToDictionary(
                redirect => redirect.Alias,
                redirect => redirect,
                StringComparer.OrdinalIgnoreCase);
        }

        public Task<bool> TryCreateAsync(Redirect redirect, CancellationToken cancellationToken)
        {
            if (_redirects.ContainsKey(redirect.Alias))
            {
                return Task.FromResult(false);
            }

            _redirects[redirect.Alias] = redirect;
            return Task.FromResult(true);
        }

        public Task<Redirect?> GetByAliasAsync(string alias, CancellationToken cancellationToken)
        {
            _redirects.TryGetValue(alias, out var redirect);
            return Task.FromResult(redirect);
        }

        public Task<IReadOnlyList<Redirect>> GetAllAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<Redirect>>(
                _redirects.Values
                    .OrderByDescending(static redirect => redirect.CreatedUtc)
                    .ThenBy(static redirect => redirect.Alias, StringComparer.OrdinalIgnoreCase)
                    .ToArray());
        }

        public Task<bool> DeleteAsync(string alias, CancellationToken cancellationToken)
        {
            return Task.FromResult(_redirects.Remove(alias));
        }
    }
}
