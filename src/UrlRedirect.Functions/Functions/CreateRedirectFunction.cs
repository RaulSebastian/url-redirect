using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using UrlRedirect.Contracts.Models;
using UrlRedirect.Domain.Model;
using UrlRedirect.Domain.Repositories;
using UrlRedirect.Domain.Validation;

namespace UrlRedirect.Functions.Functions;

public sealed class CreateRedirectFunction
{
    private readonly IRedirectRepository _redirectRepository;

    public CreateRedirectFunction(IRedirectRepository redirectRepository)
    {
        _redirectRepository = redirectRepository;
    }

    [Function("CreateRedirect")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "api/redirects")]
        HttpRequestData request,
        CancellationToken cancellationToken)
    {
        var input = await request.ReadFromJsonAsync<CreateRedirectRequest>(cancellationToken);

        if (input is null)
        {
            return await WriteJsonAsync(
                request,
                HttpStatusCode.BadRequest,
                new
                {
                    message = "A JSON request body is required."
                },
                cancellationToken);
        }

        input.Alias = RedirectRequestValidator.NormalizeAlias(input.Alias);
        input.TargetUrl = RedirectRequestValidator.NormalizeTargetUrl(input.TargetUrl);

        var validationErrors = RedirectRequestValidator.Validate(input.Alias, input.TargetUrl);

        if (validationErrors.Count > 0)
        {
            return await WriteJsonAsync(
                request,
                HttpStatusCode.BadRequest,
                new
                {
                    errors = validationErrors
                },
                cancellationToken);
        }

        var created = await _redirectRepository.TryCreateAsync(
            new Redirect(input.Alias, input.TargetUrl, DateTime.UtcNow),
            cancellationToken);

        if (!created)
        {
            return await WriteJsonAsync(
                request,
                HttpStatusCode.Conflict,
                new
                {
                    message = $"A redirect with alias '{input.Alias}' already exists."
                },
                cancellationToken);
        }

        var shortUrl = new Uri(request.Url, $"/{input.Alias}").ToString();
        var response = new CreateRedirectResponse(
            input.Alias,
            shortUrl,
            input.TargetUrl,
            (int)HttpStatusCode.Found);

        var createdResponse = request.CreateResponse(HttpStatusCode.Created);
        createdResponse.Headers.Add("Location", $"/api/redirects/{input.Alias}");
        await createdResponse.WriteAsJsonAsync(response, cancellationToken);
        return createdResponse;
    }

    private static async Task<HttpResponseData> WriteJsonAsync(
        HttpRequestData request,
        HttpStatusCode statusCode,
        object payload,
        CancellationToken cancellationToken)
    {
        var response = request.CreateResponse(statusCode);
        await response.WriteAsJsonAsync(payload, cancellationToken);
        return response;
    }
}
