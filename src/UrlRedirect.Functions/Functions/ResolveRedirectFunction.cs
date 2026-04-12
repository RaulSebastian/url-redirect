using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using UrlRedirect.Domain.Repositories;

namespace UrlRedirect.Functions.Functions;

public sealed class ResolveRedirectFunction
{
    private readonly IRedirectRepository _redirectRepository;

    public ResolveRedirectFunction(IRedirectRepository redirectRepository)
    {
        _redirectRepository = redirectRepository;
    }

    [Function("ResolveRedirect")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "{alias}")]
        HttpRequestData request,
        string alias,
        CancellationToken cancellationToken)
    {
        var canonicalAlias = alias.Trim().ToLowerInvariant();
        var redirect = await _redirectRepository.GetByAliasAsync(canonicalAlias, cancellationToken);

        if (redirect is null)
        {
            return request.CreateResponse(HttpStatusCode.NotFound);
        }

        var response = request.CreateResponse(HttpStatusCode.Found);
        response.Headers.Add("Location", redirect.TargetUrl);
        return response;
    }
}
