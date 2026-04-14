using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace UrlRedirect.Functions.Functions;

public sealed class HealthFunction
{
    private readonly HealthCheckService _healthCheckService;

    public HealthFunction(HealthCheckService healthCheckService)
    {
        _healthCheckService = healthCheckService;
    }

    [Function("Health")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")]
        HttpRequestData request,
        CancellationToken cancellationToken)
    {
        var report = await _healthCheckService.CheckHealthAsync(cancellationToken);
        var response = request.CreateResponse(
            report.Status == HealthStatus.Healthy
                ? HttpStatusCode.OK
                : HttpStatusCode.ServiceUnavailable);

        await response.WriteAsJsonAsync(
            new
            {
                status = report.Status.ToString()
            },
            cancellationToken);

        return response;
    }
}
