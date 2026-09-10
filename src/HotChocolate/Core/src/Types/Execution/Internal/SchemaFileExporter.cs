using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using HotChocolate.Serialization;

namespace HotChocolate.Execution.Internal;

internal static class SchemaFileExporter
{
    private static readonly JsonWriterOptions s_writerOptions = new()
    {
        Indented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static async Task<SchemaFileInfo> Export(
        string schemaFileName,
        IRequestExecutor executor,
        bool rewriteToSemanticNonNull,
        CancellationToken cancellationToken)
    {
        var sdl = SchemaFormatter.FormatAsString(
            executor.Schema,
            new SchemaFormatterOptions { RewriteToSemanticNonNull = rewriteToSemanticNonNull });

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

        if (directory.Length > 0)
        {
            Directory.CreateDirectory(directory);
        }

        var baseName = System.IO.Path.GetFileNameWithoutExtension(schemaFileName);
        var settingsFileName = System.IO.Path.Combine(directory, $"{baseName}-settings.json");

        await File.WriteAllTextAsync(
            schemaFileName,
            sdl + Environment.NewLine,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true),
            cancellationToken);

        var capabilities = executor.Schema
            .GetRootServiceProvider()
            .GetService<ITransportCapabilitiesProvider>()
            ?.GetCapabilities(executor.Schema.Name)
            ?? new TransportCapabilities(VariableBatching: true, RequestBatching: true);

        await WriteSettingsFile(
            settingsFileName,
            executor.Schema.Name,
            capabilities,
            cancellationToken);

        return new SchemaFileInfo(schemaFileName, settingsFileName);
    }

    private static async Task WriteSettingsFile(
        string fileName,
        string schemaName,
        TransportCapabilities capabilities,
        CancellationToken cancellationToken)
    {
        if (!await TryUpdateSettingsFile(fileName, schemaName, cancellationToken))
        {
            await CreateNewSettingsFile(fileName, schemaName, capabilities, cancellationToken);
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
                await using var writer = new Utf8JsonWriter(writeStream, s_writerOptions);
                root.WriteTo(writer);
                await writer.FlushAsync(cancellationToken);
                await writeStream.WriteAsync(Encoding.UTF8.GetBytes(Environment.NewLine), cancellationToken);
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
        TransportCapabilities capabilities,
        CancellationToken cancellationToken)
    {
        await using var settingsFileStream = File.Create(fileName);
        await using var jsonWriter = new Utf8JsonWriter(settingsFileStream, s_writerOptions);

        jsonWriter.WriteStartObject();

        jsonWriter.WriteString("name", schemaName);

        jsonWriter.WriteStartObject("transports");

        jsonWriter.WriteStartObject("http");

        jsonWriter.WriteString("url", "http://localhost:5000/graphql");

        // The exported template declares the transport extensions the server accepts
        // instead of leaving the gateway on the defaults.
        jsonWriter.WriteStartObject("capabilities");

        jsonWriter.WriteStartObject("batching");
        jsonWriter.WriteBoolean("variableBatching", capabilities.VariableBatching);
        jsonWriter.WriteBoolean("requestBatching", capabilities.RequestBatching);
        jsonWriter.WriteBoolean("aliasBatching", true);
        jsonWriter.WriteEndObject();

        jsonWriter.WriteString("onError", "propagate");

        jsonWriter.WriteEndObject();

        jsonWriter.WriteEndObject();

        jsonWriter.WriteEndObject();

        jsonWriter.WriteEndObject();

        await jsonWriter.FlushAsync(cancellationToken);
        await settingsFileStream.WriteAsync(Encoding.UTF8.GetBytes(Environment.NewLine), cancellationToken);
    }
}

internal readonly record struct SchemaFileInfo(string SchemaFileName, string SettingsFileName);
