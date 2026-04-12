using System.Text.Json;
using UrlRedirect.Web.Models;

namespace UrlRedirect.Web.Services;

public sealed class JsonRedirectStore : IRedirectStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly SemaphoreSlim _mutex = new(1, 1);
    private readonly string _storagePath;

    public JsonRedirectStore(IWebHostEnvironment environment)
    {
        var dataDirectory = Path.Combine(environment.ContentRootPath, "App_Data");
        Directory.CreateDirectory(dataDirectory);
        _storagePath = Path.Combine(dataDirectory, "redirects.json");
    }

    public async Task<bool> TryCreateAsync(RedirectRecord record, CancellationToken cancellationToken)
    {
        await _mutex.WaitAsync(cancellationToken);

        try
        {
            var redirects = await ReadAllAsync(cancellationToken);

            if (redirects.ContainsKey(record.Alias))
            {
                return false;
            }

            redirects[record.Alias] = record;

            await using var stream = File.Create(_storagePath);
            await JsonSerializer.SerializeAsync(stream, redirects, SerializerOptions, cancellationToken);

            return true;
        }
        finally
        {
            _mutex.Release();
        }
    }

    private async Task<Dictionary<string, RedirectRecord>> ReadAllAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_storagePath))
        {
            return new Dictionary<string, RedirectRecord>(StringComparer.Ordinal);
        }

        await using var stream = File.OpenRead(_storagePath);
        var redirects = await JsonSerializer.DeserializeAsync<Dictionary<string, RedirectRecord>>(
            stream,
            SerializerOptions,
            cancellationToken);

        return redirects ?? new Dictionary<string, RedirectRecord>(StringComparer.Ordinal);
    }
}
