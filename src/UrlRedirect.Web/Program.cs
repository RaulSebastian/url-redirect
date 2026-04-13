using Microsoft.Extensions.FileProviders;
using UrlRedirect.Contracts.Models;
using UrlRedirect.Domain.Model;
using UrlRedirect.Domain.Repositories;
using UrlRedirect.Domain.Validation;
using UrlRedirect.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AddPageRoute("/Index", "/ui");
});
builder.Services.AddRedirectInfrastructure(builder.Configuration, builder.Environment);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(builder.Environment.WebRootPath, "assets")),
    RequestPath = "/ui/assets"
});
app.MapGet("/", () => Results.Redirect("/ui"));
app.MapRazorPages();

app.MapPost(
    "/api/redirects",
    async Task<IResult> (
        CreateRedirectRequest? input,
        IRedirectRepository redirectRepository,
        HttpContext httpContext,
        CancellationToken cancellationToken) =>
    {
        if (input is null)
        {
            return Results.BadRequest(new { message = "A JSON request body is required." });
        }

        var alias = RedirectRequestValidator.NormalizeAlias(input.Alias ?? string.Empty);
        var targetUrl = RedirectRequestValidator.NormalizeTargetUrl(input.TargetUrl ?? string.Empty);
        var validationErrors = RedirectRequestValidator.Validate(alias, targetUrl);

        if (validationErrors.Count > 0)
        {
            return Results.BadRequest(new { errors = validationErrors });
        }

        var created = await redirectRepository.TryCreateAsync(
            new Redirect(alias, targetUrl, DateTime.UtcNow),
            cancellationToken);

        if (!created)
        {
            return Results.Conflict(new { message = $"A redirect with alias '{alias}' already exists." });
        }

        var shortUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/{alias}";
        var response = new CreateRedirectResponse(alias, shortUrl, targetUrl, (int)System.Net.HttpStatusCode.Found);

        return Results.Created($"/api/redirects/{alias}", response);
    });

app.MapGet(
    "/{alias}",
    async Task<IResult> (
        string alias,
        IRedirectRepository redirectRepository,
        CancellationToken cancellationToken) =>
    {
        var canonicalAlias = RedirectRequestValidator.NormalizeAlias(alias);

        if (string.IsNullOrWhiteSpace(canonicalAlias))
        {
            return Results.NotFound();
        }

        var redirect = await redirectRepository.GetByAliasAsync(canonicalAlias, cancellationToken);
        return redirect is null
            ? Results.NotFound()
            : Results.Redirect(redirect.TargetUrl, permanent: false, preserveMethod: false);
    });

app.Run();

public partial class Program;
