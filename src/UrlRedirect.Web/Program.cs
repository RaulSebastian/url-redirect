using UrlRedirect.Domain.Repositories;
using UrlRedirect.Domain.Validation;
using UrlRedirect.Infrastructure;
using Microsoft.Extensions.FileProviders;

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
}

app.UseHttpsRedirection();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(builder.Environment.WebRootPath, "assets")),
    RequestPath = "/ui/assets"
});
app.MapGet("/", () => Results.Redirect("/ui"));
app.MapRazorPages();

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
