using Microsoft.AspNetCore.Mvc;
using UrlRedirect.Web.Models;
using UrlRedirect.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddSingleton<IRedirectStore, JsonRedirectStore>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.MapPost("/api/redirects", async (
    [FromBody] CreateRedirectInput input,
    HttpContext httpContext,
    IRedirectStore redirectStore,
    CancellationToken cancellationToken) =>
{
    var validationErrors = CreateRedirectRequestValidator.Validate(input);

    if (validationErrors.Count > 0)
    {
        return Results.ValidationProblem(validationErrors, statusCode: StatusCodes.Status400BadRequest);
    }

    var record = new RedirectRecord(
        input.Alias,
        input.TargetUrl,
        DateTime.UtcNow);

    var created = await redirectStore.TryCreateAsync(record, cancellationToken);

    if (!created)
    {
        return Results.Conflict(new
        {
            message = $"A redirect with alias '{input.Alias}' already exists."
        });
    }

    var shortUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/{input.Alias}";
    var response = new CreateRedirectResponse(
        input.Alias,
        shortUrl,
        input.TargetUrl,
        StatusCodes.Status302Found);

    return Results.Created($"/api/redirects/{input.Alias}", response);
});

app.MapRazorPages();

app.Run();
