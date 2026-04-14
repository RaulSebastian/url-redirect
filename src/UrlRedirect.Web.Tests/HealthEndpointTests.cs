using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Xunit;

namespace UrlRedirect.Web.Tests;

public sealed class HealthEndpointTests : IClassFixture<RedirectApplicationFactory>
{
    private readonly RedirectApplicationFactory _factory;

    public HealthEndpointTests(RedirectApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsOk_WhenDependenciesAreHealthy()
    {
        using var factory = _factory.WithServices(services =>
        {
            services.PostConfigure<HealthCheckServiceOptions>(options =>
            {
                options.Registrations.Clear();
                options.Registrations.Add(new HealthCheckRegistration(
                    "test-health",
                    _ => new DelegateHealthCheck(() => HealthCheckResult.Healthy()),
                    null,
                    null));
            });
        });

        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private sealed class DelegateHealthCheck : IHealthCheck
    {
        private readonly Func<HealthCheckResult> _resultFactory;

        public DelegateHealthCheck(Func<HealthCheckResult> resultFactory)
        {
            _resultFactory = resultFactory;
        }

        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_resultFactory());
        }
    }
}
