using System.Buffers;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Text.Json;

namespace HotChocolate.Fusion.Packaging.Serializers;

internal static class SignatureManifestSerializer
{
    public static void Format(
        SignatureManifest signatureManifest,
        IBufferWriter<byte> writer,
        bool writeManifestHash = false)
    {
        using var jsonWriter = new Utf8JsonWriter(writer);

        jsonWriter.WriteStartObject();

        jsonWriter.WriteString("version", signatureManifest.Version);
        jsonWriter.WriteString("algorithm", signatureManifest.Algorithm);
        jsonWriter.WriteString("timestamp", signatureManifest.Timestamp.ToString("O"));
        jsonWriter.WriteStartObject("files");

        foreach (var file in signatureManifest.Files)
        {
            jsonWriter.WriteString(file.Key, file.Value);
        }

        jsonWriter.WriteEndObject();

        if (writeManifestHash)
        {
            jsonWriter.WriteString("manifestHash", signatureManifest.ManifestHash);
        }

        jsonWriter.Flush();
    }

    public static SignatureManifest Parse(ReadOnlyMemory<byte> data)
    {
        var document = JsonDocument.Parse(data);
        var root = document.RootElement;

        if (root.ValueKind is not JsonValueKind.Object)
        {
            throw new JsonException("Invalid signature manifest format.");
        }

        var versionProp = root.GetProperty("version");
        var algorithmProp = root.GetProperty("algorithm");
        var timestampProp = root.GetProperty("timestamp");
        var filesProp = root.GetProperty("files");

        if (versionProp.ValueKind is not JsonValueKind.String)
        {
            throw new JsonException("The signature manifest must contain a version property.");
        }

        if (algorithmProp.ValueKind is not JsonValueKind.String)
        {
            throw new JsonException("The signature manifest must contain a algorithm property.");
        }

        if (timestampProp.ValueKind is not JsonValueKind.String)
        {
            throw new JsonException("The signature manifest must contain a timestamp property.");
        }

        if (filesProp.ValueKind is not JsonValueKind.Object)
        {
            throw new JsonException("The signature manifest must contain a files property.");
        }

        var files = ImmutableDictionary.CreateBuilder<string, string>();

        foreach (var file in filesProp.EnumerateObject())
        {
            var fileName = file.Value.GetString() ?? throw new JsonException("Invalid file name.");
            files.Add(file.Name, fileName);
        }

        return new SignatureManifest
        {
            Version = versionProp.GetString()
                ?? throw new JsonException("Invalid version."),
            Algorithm = algorithmProp.GetString()
                ?? throw new JsonException("Invalid algorithm."),
            Timestamp = DateTime.Parse(timestampProp.GetString()
                ?? throw new JsonException("Invalid timestamp.")),
            ManifestHash = root.GetProperty("manifestHash").GetString()
                ?? throw new JsonException("Invalid manifest hash."),
            Files = files.ToImmutable()
        };
    }
}
