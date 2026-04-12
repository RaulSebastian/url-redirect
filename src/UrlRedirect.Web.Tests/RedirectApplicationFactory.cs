using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace UrlRedirect.Web.Tests;

public sealed class RedirectApplicationFactory : WebApplicationFactory<Program>, IDisposable
{
    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), "url-redirect-tests", Guid.NewGuid().ToString("N"));

    public string StoragePath => Path.Combine(_tempDirectory, "redirects.json");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RedirectStorage:JsonPath"] = StoragePath
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
        {
            return;
        }

        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
