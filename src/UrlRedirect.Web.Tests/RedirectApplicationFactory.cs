using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace UrlRedirect.Web.Tests;

public sealed class RedirectApplicationFactory : WebApplicationFactory<Program>, IDisposable
{
    private readonly Action<IServiceCollection>? _configureServices;
    private readonly bool _authenticateAsAdmin;

    public RedirectApplicationFactory()
    {
    }

    private RedirectApplicationFactory(Action<IServiceCollection>? configureServices, bool authenticateAsAdmin)
    {
        _configureServices = configureServices;
        _authenticateAsAdmin = authenticateAsAdmin;
    }

    public RedirectApplicationFactory WithServices(Action<IServiceCollection> configureServices)
    {
        return new RedirectApplicationFactory(configureServices, _authenticateAsAdmin);
    }

    public RedirectApplicationFactory WithAuthenticatedAdmin()
    {
        return new RedirectApplicationFactory(_configureServices, authenticateAsAdmin: true);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:AzureAd:Instance"] = "https://login.microsoftonline.com/",
                ["Authentication:AzureAd:TenantId"] = "test-tenant-id",
                ["Authentication:AzureAd:ClientId"] = "test-client-id",
                ["Authentication:AzureAd:ClientSecret"] = "test-client-secret",
                ["Authorization:Admin:AllowedRoles:0"] = "UrlRedirect.Admin"
            });
        });

        if (_configureServices is not null)
        {
            builder.ConfigureServices(_configureServices);
        }

        builder.ConfigureTestServices(services =>
        {
            services.PostConfigureAll<OpenIdConnectOptions>(options =>
            {
                var configuration = new OpenIdConnectConfiguration
                {
                    AuthorizationEndpoint = "https://login.microsoftonline.com/test-tenant-id/oauth2/v2.0/authorize",
                    EndSessionEndpoint = "https://login.microsoftonline.com/test-tenant-id/oauth2/v2.0/logout",
                    Issuer = "https://login.microsoftonline.com/test-tenant-id/v2.0",
                    TokenEndpoint = "https://example.test/token"
                };

                options.Configuration = configuration;
                options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(configuration);
            });
        });

        if (_authenticateAsAdmin)
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddAuthentication("Test")
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
                services.PostConfigureAll<AuthenticationOptions>(options =>
                {
                    options.DefaultAuthenticateScheme = "Test";
                    options.DefaultChallengeScheme = "Test";
                    options.DefaultForbidScheme = "Test";
                    options.DefaultScheme = "Test";
                    options.DefaultSignInScheme = "Test";
                    options.DefaultSignOutScheme = "Test";
                });
            });
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
    }
}
