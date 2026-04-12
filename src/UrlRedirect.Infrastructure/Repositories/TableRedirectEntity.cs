using Azure;
using Azure.Data.Tables;

namespace UrlRedirect.Infrastructure.Repositories;

internal sealed class TableRedirectEntity : ITableEntity
{
    public string PartitionKey { get; set; } = "redirect";

    public string RowKey { get; set; } = string.Empty;

    public string TargetUrl { get; set; } = string.Empty;

    public DateTime CreatedUtc { get; set; }

    public DateTimeOffset? Timestamp { get; set; }

    public ETag ETag { get; set; }
}
