using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace HotChocolate.Execution.Internal;

internal static class SchemaFileExporter
{
    public static async Task Export(
        string schemaFileName,
        IRequestExecutor executor,
        CancellationToken cancellationToken)
    {
        var sdl = executor.Schema.ToString();

        if (Directory.Exists(schemaFileName))
        {
            schemaFileName = System.IO.Path.Combine(schemaFileName, "schema.graphqls");
        }

        var schemaFileExtension = System.IO.Path.GetExtension(schemaFileName);

        if (schemaFileExtension is not ".graphql" and not ".graphqls")
        {
            schemaFileName += ".graphqls";
        }

        var directory = System.IO.Path.GetDirectoryName(schemaFileName)!;

        if (Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(
            schemaFileName,
            sdl,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true),
            cancellationToken);

        await WriteSettingsFile(schemaFileName, executor.Schema.Name, cancellationToken);
    }

    private static async Task WriteSettingsFile(
        string schemaFileName,
        string schemaName,
        CancellationToken cancellationToken)
    {
        var dir = System.IO.Path.GetDirectoryName(schemaFileName)!;
        var baseName = System.IO.Path.GetFileNameWithoutExtension(schemaFileName);
        var fileName = System.IO.Path.Combine(dir, $"{baseName}-settings.json");

        if (!await TryUpdateSettingsFile(fileName, schemaName, cancellationToken))
        {
            await CreateNewSettingsFile(fileName, schemaName, cancellationToken);
        }
    }

    private static async Task<bool> TryUpdateSettingsFile(
        string fileName,
        string schemaName,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(fileName))
        {
            return false;
        }

        try
        {
            JsonNode? root;
            await using (var readStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                root = await JsonNode.ParseAsync(readStream, cancellationToken: cancellationToken);
            }

            if (root is JsonObject obj && obj["name"] is not null)
            {
                obj["name"] = schemaName;

                await using var writeStream = File.Create(fileName);
                await using var writer = new Utf8JsonWriter(writeStream, new JsonWriterOptions { Indented = true });
                root.WriteTo(writer);
                await writer.FlushAsync(cancellationToken);
                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private static async Task CreateNewSettingsFile(
        string fileName,
        string schemaName,
        CancellationToken cancellationToken)
    {
        await using var settingsFileStream = File.Create(fileName);
        await using var jsonWriter = new Utf8JsonWriter(settingsFileStream, new JsonWriterOptions { Indented = true });

        jsonWriter.WriteStartObject();

        jsonWriter.WriteString("name", schemaName);

        jsonWriter.WriteStartObject("transports");

        jsonWriter.WriteStartObject("http");

        jsonWriter.WriteString("url", "http://localhost:5000/graphql");

        jsonWriter.WriteEndObject();

        jsonWriter.WriteEndObject();

        jsonWriter.WriteEndObject();

        await jsonWriter.FlushAsync(cancellationToken);
    }
}
