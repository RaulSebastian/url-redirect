using System.Net;
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
    public async Task IndexPage_LoadsFrontendThatTargetsTheFunctionsApi()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();
        var scriptResponse = await client.GetAsync("/js/site.js");
        var script = await scriptResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, scriptResponse.StatusCode);
        Assert.Contains("Create redirect", html);
        Assert.Contains("/js/site.js", html);
        Assert.Contains("/api/redirects", script);
    }
}
