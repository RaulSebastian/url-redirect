using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using UrlRedirect.Domain.Model;
using UrlRedirect.Domain.Repositories;
using Xunit;

namespace UrlRedirect.Web.Tests;

public sealed class RedirectCreationSmokeTests : IClassFixture<RedirectApplicationFactory>
{
    private readonly RedirectApplicationFactory _factory;

    public RedirectCreationSmokeTests(RedirectApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task RootPath_RedirectsToUi()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/ui", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task UiPage_LoadsFrontendThatTargetsTheFunctionsApi()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/ui");
        var html = await response.Content.ReadAsStringAsync();
        var scriptResponse = await client.GetAsync("/ui/assets/site.js");
        var script = await scriptResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, scriptResponse.StatusCode);
        Assert.Contains("Create redirect", html);
        Assert.Contains("Copy short URL", html);
        Assert.Contains("Show QR code", html);
        Assert.Contains("Redirect QR code", html);
        Assert.Contains("/admin", html);
        Assert.Contains("/api/redirects", script);
        Assert.Contains("/api/qr-code", script);
    }

    [Fact]
    public async Task AdminPage_RequiresAuthentication()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/admin");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("login.microsoftonline.com", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task AdminPage_LoadsForAuthenticatedAdmin()
    {
        using var factory = _factory.WithAuthenticatedAdmin().WithServices(services =>
        {
            services.RemoveAll<IRedirectRepository>();
            services.AddSingleton<IRedirectRepository>(new CreateTestRepository(
                new Redirect("summer-sale", "https://example.com/campaign", DateTime.UtcNow)));
        });
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/admin");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("admin console", html);
        Assert.Contains("summer-sale", html);
        Assert.Contains("Delete", html);
        Assert.Contains("Rows per page", html);
        Assert.Contains("Page 1 of 1", html);
    }

    [Fact]
    public async Task AdminPage_PaginatesRedirects_AndRespectsRequestedPageSize()
    {
        var redirects = Enumerable.Range(1, 12)
            .Select(i => new Redirect($"alias-{i:00}", $"https://example.com/{i}", DateTime.UtcNow.AddMinutes(-i)))
            .ToArray();

        using var factory = _factory.WithAuthenticatedAdmin().WithServices(services =>
        {
            services.RemoveAll<IRedirectRepository>();
            services.AddSingleton<IRedirectRepository>(new CreateTestRepository(redirects));
        });
        using var client = factory.CreateClient();

        var firstPageResponse = await client.GetAsync("/admin");
        var firstPageHtml = await firstPageResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, firstPageResponse.StatusCode);
        Assert.Contains("alias-01", firstPageHtml);
        Assert.Contains("alias-10", firstPageHtml);
        Assert.DoesNotContain("alias-11", firstPageHtml);
        Assert.Contains("Page 1 of 2", firstPageHtml);

        var secondPageResponse = await client.GetAsync("/admin?page=2&pageSize=10");
        var secondPageHtml = await secondPageResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, secondPageResponse.StatusCode);
        Assert.Contains("alias-11", secondPageHtml);
        Assert.Contains("alias-12", secondPageHtml);
        Assert.DoesNotContain("alias-10", secondPageHtml);

        var largerPageResponse = await client.GetAsync("/admin?page=1&pageSize=20");
        var largerPageHtml = await largerPageResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, largerPageResponse.StatusCode);
        Assert.Contains("alias-12", largerPageHtml);
        Assert.Contains("Page 1 of 1", largerPageHtml);
    }

    [Fact]
    public async Task CreateRedirect_AllowsAnonymousUsers()
    {
        using var factory = _factory.WithServices(services =>
        {
            services.RemoveAll<IRedirectRepository>();
            services.AddSingleton<IRedirectRepository>(new CreateTestRepository());
        });
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/redirects", new { alias = "summer-sale", targetUrl = "https://example.com/campaign" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateRedirect_ReturnsCreatedWithShortUrl_ForAuthenticatedAdmin()
    {
        using var factory = _factory.WithAuthenticatedAdmin().WithServices(services =>
        {
            services.RemoveAll<IRedirectRepository>();
            services.AddSingleton<IRedirectRepository>(new CreateTestRepository());
        });

        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/redirects", new { alias = "summer-sale", targetUrl = "https://example.com/campaign" });
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.True(body.StartsWith("{"), $"Expected JSON body, got: {body[..Math.Min(200, body.Length)]}");
        var payload = JsonDocument.Parse(body).RootElement;
        Assert.Equal("summer-sale", payload.GetProperty("alias").GetString());
        Assert.Equal("https://example.com/campaign", payload.GetProperty("targetUrl").GetString());
        Assert.EndsWith("/summer-sale", payload.GetProperty("shortUrl").GetString());
        Assert.Equal(302, payload.GetProperty("statusCode").GetInt32());
    }

    [Fact]
    public async Task CreateRedirect_InvalidAlias_ReturnsBadRequestWithErrors_ForAuthenticatedAdmin()
    {
        using var factory = _factory.WithAuthenticatedAdmin().WithServices(services =>
        {
            services.RemoveAll<IRedirectRepository>();
            services.AddSingleton<IRedirectRepository>(new CreateTestRepository());
        });

        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/redirects", new { alias = "ui", targetUrl = "https://example.com" });
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(body.StartsWith("{"), $"Expected JSON body, got: {body[..Math.Min(200, body.Length)]}");
        var payload = JsonDocument.Parse(body).RootElement;
        Assert.True(payload.TryGetProperty("errors", out _));
    }

    [Fact]
    public async Task CreateRedirect_DuplicateAlias_ReturnsConflict_ForAuthenticatedAdmin()
    {
        var repository = new CreateTestRepository(new Redirect("summer-sale", "https://example.com/campaign", DateTime.UtcNow));

        using var factory = _factory.WithAuthenticatedAdmin().WithServices(services =>
        {
            services.RemoveAll<IRedirectRepository>();
            services.AddSingleton<IRedirectRepository>(repository);
        });

        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/redirects", new { alias = "summer-sale", targetUrl = "https://example.com/other" });
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.True(body.StartsWith("{"), $"Expected JSON body, got: {body[..Math.Min(200, body.Length)]}");
        var payload = JsonDocument.Parse(body).RootElement;
        Assert.True(payload.TryGetProperty("message", out _));
    }

    [Fact]
    public async Task QrCodeEndpoint_ReturnsSvg_ForAbsoluteShortUrl()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/qr-code?value=https%3A%2F%2Fexample.test%2Fsummer-sale");
        var svg = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/svg+xml; charset=utf-8", response.Content.Headers.ContentType?.ToString());
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public async Task QrCodeEndpoint_RejectsRelativeValues()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/qr-code?value=%2Fsummer-sale");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private sealed class CreateTestRepository : IRedirectRepository
    {
        private readonly Dictionary<string, Redirect> _redirects;

        public CreateTestRepository(params Redirect[] existing)
        {
            _redirects = existing.ToDictionary(r => r.Alias, StringComparer.OrdinalIgnoreCase);
        }

        public Task<bool> TryCreateAsync(Redirect redirect, CancellationToken cancellationToken)
        {
            if (_redirects.ContainsKey(redirect.Alias))
                return Task.FromResult(false);

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
