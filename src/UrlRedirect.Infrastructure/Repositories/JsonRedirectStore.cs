using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using UrlRedirect.Domain.Model;
using UrlRedirect.Domain.Repositories;

namespace UrlRedirect.Infrastructure.Repositories;

public sealed class JsonRedirectStore : IRedirectRepository
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly SemaphoreSlim _mutex = new(1, 1);
    private readonly string _storagePath;

    public JsonRedirectStore(IHostEnvironment environment, IConfiguration configuration)
    {
        _storagePath = configuration["RedirectStorage:JsonPath"]
            ?? Path.Combine(environment.ContentRootPath, "App_Data", "redirects.json");

        var dataDirectory = Path.GetDirectoryName(_storagePath)
            ?? throw new InvalidOperationException("A storage path with a directory is required.");

        Directory.CreateDirectory(dataDirectory);
    }

    public async Task<bool> TryCreateAsync(Redirect redirect, CancellationToken cancellationToken)
    {
        await _mutex.WaitAsync(cancellationToken);

        try
        {
            var redirects = await ReadAllAsync(cancellationToken);

            if (!redirects.TryAdd(redirect.Alias, redirect))
            {
                return false;
            }

            await using var stream = File.Create(_storagePath);
            await JsonSerializer.SerializeAsync(stream, redirects, SerializerOptions, cancellationToken);

            return true;
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task<Redirect?> GetByAliasAsync(string alias, CancellationToken cancellationToken)
    {
        await _mutex.WaitAsync(cancellationToken);

        try
        {
            var redirects = await ReadAllAsync(cancellationToken);
            return redirects.GetValueOrDefault(alias);
        }
        finally
        {
            _mutex.Release();
        }
    }

    private async Task<Dictionary<string, Redirect>> ReadAllAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_storagePath))
        {
            return new Dictionary<string, Redirect>(StringComparer.Ordinal);
        }

        await using var stream = File.OpenRead(_storagePath);
        var redirects = await JsonSerializer.DeserializeAsync<Dictionary<string, Redirect>>(
            stream,
            SerializerOptions,
            cancellationToken);

        return redirects ?? new Dictionary<string, Redirect>(StringComparer.Ordinal);
    }
}
