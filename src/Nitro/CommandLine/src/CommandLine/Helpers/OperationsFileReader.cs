using System.Buffers;
using System.Text.Json;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Helpers;

/// <summary>
/// Reads an operations file and returns its content in the Relay-style format,
/// a JSON object that maps operation IDs to operation documents.
/// </summary>
internal static class OperationsFileReader
{
    private const int InitialBufferSize = 4096;

    private static ReadOnlySpan<byte> Utf8Bom => [0xEF, 0xBB, 0xBF];

    private static ReadOnlySpan<byte> FormatPropertyName => "format"u8;

    private static ReadOnlySpan<byte> ApolloManifestFormat => "apollo-persisted-query-manifest"u8;

    /// <summary>
    /// Returns a stream with the operations of <paramref name="fileStream"/> and takes ownership of it.
    /// An Apollo persisted query manifest is converted to the Relay-style format,
    /// Relay-style content is validated and returned unchanged.
    /// </summary>
    public static async Task<Stream> OpenAsync(Stream fileStream, CancellationToken cancellationToken)
    {
        var stream = fileStream;

        try
        {
            if (!stream.CanSeek)
            {
                var buffer = new MemoryStream();
                await stream.CopyToAsync(buffer, cancellationToken);
                await stream.DisposeAsync();
                buffer.Position = 0;
                stream = buffer;
            }

            var start = stream.Position;
            var isApolloManifest = await InspectAsync(stream, cancellationToken);
            stream.Position = start;

            if (!isApolloManifest)
            {
                return stream;
            }

            var converted = await ConvertApolloManifestAsync(stream, cancellationToken);
            await stream.DisposeAsync();
            return converted;
        }
        catch
        {
            await stream.DisposeAsync();
            throw;
        }
    }

    /// <summary>
    /// Returns whether the content is an Apollo persisted query manifest.
    /// Throws when Relay-style content has an operation that is not a string.
    /// </summary>
    private static async Task<bool> InspectAsync(Stream stream, CancellationToken cancellationToken)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(InitialBufferSize);
        var scanner = new FormatScanner();
        var length = 0;
        var isBomChecked = false;

        try
        {
            while (true)
            {
                if (length == buffer.Length)
                {
                    var larger = ArrayPool<byte>.Shared.Rent(buffer.Length * 2);
                    buffer.AsSpan(0, length).CopyTo(larger);
                    ArrayPool<byte>.Shared.Return(buffer);
                    buffer = larger;
                }

                var read = await stream.ReadAsync(buffer.AsMemory(length), cancellationToken);
                length += read;
                var isFinalBlock = read == 0;

                if (!isBomChecked)
                {
                    if (length < Utf8Bom.Length && !isFinalBlock)
                    {
                        continue;
                    }

                    if (buffer.AsSpan(0, length).StartsWith(Utf8Bom))
                    {
                        buffer.AsSpan(Utf8Bom.Length, length - Utf8Bom.Length).CopyTo(buffer);
                        length -= Utf8Bom.Length;
                    }

                    isBomChecked = true;
                }

                var result = scanner.Scan(buffer.AsSpan(0, length), isFinalBlock, out var consumed);

                if (result.HasValue)
                {
                    return result.Value;
                }

                if (isFinalBlock)
                {
                    break;
                }

                buffer.AsSpan(consumed, length - consumed).CopyTo(buffer);
                length -= consumed;
            }
        }
        catch (JsonException)
        {
            // Malformed JSON is not a manifest and is returned unchanged.
            return false;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }

        if (scanner.InvalidOperationId is { } operationId)
        {
            throw InvalidRelayOperation(operationId.EscapeMarkup());
        }

        return false;
    }

    private static async Task<MemoryStream> ConvertApolloManifestAsync(
        Stream stream,
        CancellationToken cancellationToken)
    {
        JsonDocument document;

        try
        {
            document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        }
        catch (JsonException ex)
        {
            throw InvalidApolloManifest(ex.Message.EscapeMarkup());
        }

        using var _ = document;
        var root = document.RootElement;

        if (!root.TryGetProperty("version", out var version))
        {
            throw InvalidApolloManifest("The 'version' property is missing.");
        }

        if (version.ValueKind != JsonValueKind.Number
            || !version.TryGetInt32(out var versionNumber)
            || versionNumber != 1)
        {
            throw InvalidApolloManifest(
                $"Version '{version.GetRawText().EscapeMarkup()}' is not supported. Expected version '1'.");
        }

        if (!root.TryGetProperty("operations", out var operations)
            || operations.ValueKind != JsonValueKind.Array)
        {
            throw InvalidApolloManifest("The 'operations' property must be an array.");
        }

        var output = new MemoryStream();

        await using (var writer = new Utf8JsonWriter(output))
        {
            writer.WriteStartObject();

            var index = 0;

            foreach (var operation in operations.EnumerateArray())
            {
                if (!TryGetString(operation, "id", out var id)
                    || !TryGetString(operation, "body", out var body))
                {
                    throw InvalidApolloManifest(
                        $"The operation at index {index} must have a string 'id' and a string 'body'.");
                }

                writer.WriteString(id, body);
                index++;
            }

            writer.WriteEndObject();
        }

        output.Position = 0;
        return output;
    }

    private static bool TryGetString(JsonElement element, string propertyName, out string value)
    {
        if (element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty(propertyName, out var property)
            && property.ValueKind == JsonValueKind.String)
        {
            value = property.GetString()!;
            return true;
        }

        value = string.Empty;
        return false;
    }

    private struct FormatScanner
    {
        private JsonReaderState _state;

        /// <summary>
        /// Gets the ID of the first root property whose value is not a string,
        /// or <c>null</c> when every value scanned so far is a string.
        /// </summary>
        public string? InvalidOperationId { get; private set; }

        /// <summary>
        /// Scans the next block of JSON and returns whether the root object declares the
        /// Apollo manifest format, or <c>null</c> when more data is needed.
        /// </summary>
        public bool? Scan(ReadOnlySpan<byte> json, bool isFinalBlock, out int consumed)
        {
            var reader = new Utf8JsonReader(json, isFinalBlock, _state);

            while (true)
            {
                consumed = (int)reader.BytesConsumed;
                _state = reader.CurrentState;

                if (!reader.Read())
                {
                    return null;
                }

                if (reader.CurrentDepth == 0
                    && reader.TokenType is not JsonTokenType.StartObject and not JsonTokenType.EndObject)
                {
                    return false;
                }

                if (reader.CurrentDepth != 1 || reader.TokenType != JsonTokenType.PropertyName)
                {
                    continue;
                }

                var name = reader;

                // A name without its value is read again with the next block.
                if (!reader.Read())
                {
                    return null;
                }

                if (reader.TokenType != JsonTokenType.String)
                {
                    InvalidOperationId ??= name.GetString();
                }
                else if (name.ValueTextEquals(FormatPropertyName)
                    && reader.ValueTextEquals(ApolloManifestFormat))
                {
                    return true;
                }
            }
        }
    }
}
