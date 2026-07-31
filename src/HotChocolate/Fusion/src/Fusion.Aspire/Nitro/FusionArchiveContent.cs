using System.Buffers;
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HotChocolate.Fusion.SourceSchema.Packaging;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Aspire.Nitro;

internal sealed record FusionArchiveContent(
    string Schema,
    string? SchemaExtensions,
    string Settings)
{
    private static ReadOnlySpan<byte> DigestDomain
        => "HotChocolate.Fusion.Aspire.SourceSchemaContent.v1"u8;

    public static async Task<FusionArchiveContent> ReadAsync(
        string archivePath,
        string expectedName,
        CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(archivePath);
        return await ReadAsync(stream, expectedName, cancellationToken);
    }

    public static async Task<FusionArchiveContent> ReadAsync(
        Stream stream,
        string expectedName,
        CancellationToken cancellationToken)
    {
        try
        {
            using var archive = FusionSourceSchemaArchive.Open(
                stream,
                leaveOpen: true);
            var schema = await archive.TryGetSchemaAsync(cancellationToken)
                ?? throw new FusionDeploymentException(
                    "The Fusion source schema archive contains no schema.");
            var extensions =
                await archive.TryGetSchemaExtensionsAsync(cancellationToken);
            using var settings = await archive.TryGetSettingsAsync(cancellationToken)
                ?? throw new FusionDeploymentException(
                    "The Fusion source schema archive contains no settings.");

            if (!settings.RootElement.TryGetProperty("name", out var name)
                || name.ValueKind is not JsonValueKind.String
                || !string.Equals(
                    name.GetString(),
                    expectedName,
                    StringComparison.Ordinal))
            {
                throw new FusionDeploymentException(
                    "The source schema settings name must exactly match "
                    + $"'{expectedName}'.");
            }

            return new FusionArchiveContent(
                NormalizeGraphQL(schema),
                extensions is null
                    ? null
                    : NormalizeGraphQL(extensions.Value),
                NormalizeJson(settings.RootElement));
        }
        catch (FusionDeploymentException)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is InvalidDataException
                or InvalidOperationException
                or JsonException
                or SyntaxException)
        {
            throw new FusionDeploymentException(
                "The Fusion source schema archive is invalid.",
                exception);
        }
    }

    public string ComputeSha256(CancellationToken cancellationToken)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        hash.AppendData(DigestDomain);
        Append(hash, Schema, cancellationToken);
        Append(hash, SchemaExtensions, cancellationToken);
        Append(hash, Settings, cancellationToken);
        return Convert.ToHexString(hash.GetHashAndReset());
    }

    private static string NormalizeGraphQL(ReadOnlyMemory<byte> source)
        => Utf8GraphQLParser.Parse(source.Span).ToString();

    private static string NormalizeJson(JsonElement value)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            WriteCanonicalJson(writer, value);
        }

        return Encoding.UTF8.GetString(stream.GetBuffer(), 0, (int)stream.Length);
    }

    private static void WriteCanonicalJson(
        Utf8JsonWriter writer,
        JsonElement value)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in value.EnumerateObject()
                    .OrderBy(property => property.Name, StringComparer.Ordinal))
                {
                    writer.WritePropertyName(property.Name);
                    WriteCanonicalJson(writer, property.Value);
                }
                writer.WriteEndObject();
                break;

            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in value.EnumerateArray())
                {
                    WriteCanonicalJson(writer, item);
                }
                writer.WriteEndArray();
                break;

            case JsonValueKind.String:
                writer.WriteStringValue(value.GetString());
                break;

            case JsonValueKind.Number:
                writer.WriteRawValue(value.GetRawText());
                break;

            case JsonValueKind.True:
                writer.WriteBooleanValue(true);
                break;

            case JsonValueKind.False:
                writer.WriteBooleanValue(false);
                break;

            case JsonValueKind.Null:
                writer.WriteNullValue();
                break;

            default:
                throw new JsonException(
                    $"Unsupported JSON token kind '{value.ValueKind}'.");
        }
    }

    private static void Append(
        IncrementalHash hash,
        string? value,
        CancellationToken cancellationToken)
    {
        Span<byte> header = stackalloc byte[9];
        header[0] = value is null ? (byte)0 : (byte)1;
        BinaryPrimitives.WriteInt64BigEndian(
            header[1..],
            value is null ? 0 : Encoding.UTF8.GetByteCount(value));
        hash.AppendData(header);

        if (value is null)
        {
            return;
        }

        var buffer = ArrayPool<byte>.Shared.Rent(4096);
        try
        {
            var encoder = Encoding.UTF8.GetEncoder();
            var remaining = value.AsSpan();
            var completed = false;

            while (!completed)
            {
                cancellationToken.ThrowIfCancellationRequested();
                encoder.Convert(
                    remaining,
                    buffer,
                    flush: true,
                    out var charsUsed,
                    out var bytesUsed,
                    out completed);
                hash.AppendData(buffer.AsSpan(0, bytesUsed));
                remaining = remaining[charsUsed..];
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
