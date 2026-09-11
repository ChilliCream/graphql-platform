using System.Buffers;
using System.Collections.Immutable;
using System.Text.Json;

namespace HotChocolate.Fusion.Packaging.Serializers;

internal static class ArchiveManifestSerializer
{
    // A fixed writer configuration keeps the serialized bytes reproducible for identical content.
    private static readonly JsonWriterOptions s_writerOptions = new() { Indented = false };

    public static void Format(ArchiveManifest manifest, IBufferWriter<byte> writer)
    {
        using var jsonWriter = new Utf8JsonWriter(writer, s_writerOptions);

        jsonWriter.WriteStartObject();

        jsonWriter.WriteString("version", manifest.Version);
        jsonWriter.WriteString("algorithm", manifest.Algorithm);

        jsonWriter.WriteStartObject("files");

        foreach (var file in manifest.Files.OrderBy(t => t.Key, StringComparer.Ordinal))
        {
            jsonWriter.WriteString(file.Key, file.Value);
        }

        jsonWriter.WriteEndObject();

        jsonWriter.WriteStartObject("artifacts");

        foreach (var artifact in manifest.Artifacts.OrderBy(t => t.Key, StringComparer.Ordinal))
        {
            jsonWriter.WriteString(artifact.Key, artifact.Value);
        }

        jsonWriter.WriteEndObject();

        jsonWriter.WriteEndObject();
        jsonWriter.Flush();
    }

    public static ArchiveManifest Parse(ReadOnlyMemory<byte> data)
    {
        using var document = JsonDocument.Parse(data);
        var root = document.RootElement;

        if (root.ValueKind is not JsonValueKind.Object)
        {
            throw new JsonException("Invalid content manifest format.");
        }

        var versionProp = root.GetProperty("version");
        if (versionProp.ValueKind is not JsonValueKind.String)
        {
            throw new JsonException("The content manifest must contain a version property.");
        }

        var algorithmProp = root.GetProperty("algorithm");
        if (algorithmProp.ValueKind is not JsonValueKind.String)
        {
            throw new JsonException("The content manifest must contain an algorithm property.");
        }

        var filesProp = root.GetProperty("files");
        if (filesProp.ValueKind is not JsonValueKind.Object)
        {
            throw new JsonException("The content manifest must contain a files property.");
        }

        var artifactsProp = root.GetProperty("artifacts");
        if (artifactsProp.ValueKind is not JsonValueKind.Object)
        {
            throw new JsonException("The content manifest must contain an artifacts property.");
        }

        var files = ImmutableSortedDictionary.CreateBuilder<string, string>(StringComparer.Ordinal);
        foreach (var file in filesProp.EnumerateObject())
        {
            files.Add(file.Name, file.Value.GetString() ?? throw new JsonException("Invalid file digest."));
        }

        var artifacts = ImmutableSortedDictionary.CreateBuilder<string, string>(StringComparer.Ordinal);
        foreach (var artifact in artifactsProp.EnumerateObject())
        {
            artifacts.Add(
                artifact.Name,
                artifact.Value.GetString() ?? throw new JsonException("Invalid artifact digest."));
        }

        return new ArchiveManifest
        {
            Version = versionProp.GetString()!,
            Algorithm = algorithmProp.GetString()!,
            Files = files.ToImmutable(),
            Artifacts = artifacts.ToImmutable()
        };
    }
}
