using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
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
    public async Task CreateRedirect_PersistsMappingAndReturnsShortUrl()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://go.test")
        });

        var response = await client.PostAsJsonAsync("/api/redirects", new
        {
            Alias = "summer-sale",
            TargetUrl = "https://example.com/campaign"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<CreateRedirectResponseContract>();

        Assert.NotNull(payload);
        Assert.Equal("summer-sale", payload.Alias);
        Assert.Equal("https://example.com/campaign", payload.TargetUrl);
        Assert.Equal(302, payload.StatusCode);
        Assert.Equal("https://go.test/summer-sale", payload.ShortUrl);

        Assert.True(File.Exists(_factory.StoragePath));

        await using var stream = File.OpenRead(_factory.StoragePath);
        var storedRedirects = await JsonSerializer.DeserializeAsync<Dictionary<string, StoredRedirectContract>>(
            stream,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(storedRedirects);
        Assert.True(storedRedirects.ContainsKey("summer-sale"));
        Assert.Equal("https://example.com/campaign", storedRedirects["summer-sale"].TargetUrl);
    }

    public sealed record CreateRedirectResponseContract(
        string Alias,
        string ShortUrl,
        string TargetUrl,
        int StatusCode);

    public sealed record StoredRedirectContract(
        string Alias,
        string TargetUrl,
        DateTime CreatedUtc);
}
