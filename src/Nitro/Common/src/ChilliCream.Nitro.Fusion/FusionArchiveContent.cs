using System.Text;
using System.Text.Json;
using HotChocolate.Fusion.SourceSchema.Packaging;
using HotChocolate.Language;

namespace ChilliCream.Nitro.Fusion;

internal sealed record FusionArchiveContent(
    string Schema,
    string? SchemaExtensions,
    string Settings)
{
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
                or JsonException
                or SyntaxException)
        {
            throw new FusionDeploymentException(
                "The Fusion source schema archive is invalid.",
                exception);
        }
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
}
