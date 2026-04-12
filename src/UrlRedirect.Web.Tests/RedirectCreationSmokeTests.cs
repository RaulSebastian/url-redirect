using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
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
        Assert.Contains("/ui/assets/site.js", html);
        Assert.Contains("/api/redirects", script);
    }
}
