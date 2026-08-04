using System.Text;
using HotChocolate.Buffers;
using static HotChocolate.Fusion.Properties.CompositionResources;

namespace HotChocolate.Fusion;

internal static class SchemaHttpResponseReader
{
    internal const int MaxResponseSize = 50_000_000;

    public static async Task<string> ReadAsStringAsync(
        HttpContent content,
        string sourceSchemaName,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrEmpty(sourceSchemaName);

        if (content.Headers.ContentLength is > MaxResponseSize)
        {
            throw ResponseTooLarge(sourceSchemaName);
        }

        await using var stream = await content.ReadAsStreamAsync(cancellationToken);
        using var buffer = new PooledArrayWriter();

        while (true)
        {
            var remaining = MaxResponseSize - buffer.Length;
            var readSize = Math.Min(81920, remaining + 1);
            var memory = buffer.GetMemory(readSize)[..readSize];
            var bytesRead = await stream.ReadAsync(memory, cancellationToken);

            if (bytesRead == 0)
            {
                break;
            }

            buffer.Advance(bytesRead);

            if (buffer.Length > MaxResponseSize)
            {
                throw ResponseTooLarge(sourceSchemaName);
            }
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    private static InvalidOperationException ResponseTooLarge(string sourceSchemaName)
        => new(string.Format(
            SchemaHttpResponseReader_ResponseTooLarge,
            sourceSchemaName,
            MaxResponseSize));
}
