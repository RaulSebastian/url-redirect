using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.FileProviders;
using Microsoft.Identity.Web;
using UrlRedirect.Contracts.Models;
using UrlRedirect.Domain.Model;
using UrlRedirect.Domain.Repositories;
using UrlRedirect.Domain.Validation;
using UrlRedirect.Infrastructure;
using UrlRedirect.Web.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationInsightsTelemetry();
builder.Services.Configure<AdminAuthorizationOptions>(
    builder.Configuration.GetSection(AdminAuthorizationOptions.SectionName));
builder.Services.AddSingleton<IAuthorizationHandler, AdminAccessHandler>();
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("Authentication:AzureAd"));
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AddPageRoute("/Index", "/ui");
    options.Conventions.AuthorizeFolder("/Admin", AdminPolicies.AdminOnly);
});
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AdminPolicies.AdminOnly, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.AddRequirements(new AdminAccessRequirement());
    });
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
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/", () => Results.Redirect("/ui"));
app.MapHealthChecks("/health");

app.MapGet(
    "/auth/login",
    (string? returnUrl) => Results.Challenge(
        new AuthenticationProperties
        {
            RedirectUri = NormalizeReturnUrl(returnUrl)
        },
        [OpenIdConnectDefaults.AuthenticationScheme]));

app.MapGet(
    "/auth/logout",
    (string? returnUrl) => Results.SignOut(
        new AuthenticationProperties
        {
            RedirectUri = NormalizeReturnUrl(returnUrl, "/ui")
        },
        [CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme]));

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

static string NormalizeReturnUrl(string? returnUrl, string fallbackPath = "/admin")
{
    return string.IsNullOrWhiteSpace(returnUrl) || !Uri.TryCreate(returnUrl, UriKind.Relative, out _)
        ? fallbackPath
        : returnUrl;
}

public partial class Program;
