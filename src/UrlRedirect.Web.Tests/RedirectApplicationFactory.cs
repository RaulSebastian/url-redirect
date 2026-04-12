using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace UrlRedirect.Web.Tests;

public sealed class RedirectApplicationFactory : WebApplicationFactory<Program>, IDisposable
{
    private readonly Action<IServiceCollection>? _configureServices;

    public RedirectApplicationFactory()
    {
    }

    private RedirectApplicationFactory(Action<IServiceCollection>? configureServices)
    {
        _configureServices = configureServices;
    }

    public RedirectApplicationFactory WithServices(Action<IServiceCollection> configureServices)
    {
        return new RedirectApplicationFactory(configureServices);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        if (_configureServices is not null)
        {
            builder.ConfigureServices(_configureServices);
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
    }
}
