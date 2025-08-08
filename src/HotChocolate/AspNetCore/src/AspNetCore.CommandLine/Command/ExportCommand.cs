using System.CommandLine;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HotChocolate.AspNetCore.CommandLine;

/// <summary>
/// The export command can be used to export the schema to a file.
/// </summary>
internal sealed class ExportCommand : Command
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExportCommand"/> class.
    /// </summary>
    public ExportCommand() : base("export")
    {
        Description =
            "Export the graphql schema. If no output (--output) is specified the schema will be "
            + "printed to the console.";

        AddOption(Opt<OutputOption>.Instance);
        AddOption(Opt<SchemaNameOption>.Instance);

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<IConsole>(),
            Bind.FromServiceProvider<IHost>(),
            Opt<OutputOption>.Instance,
            Opt<SchemaNameOption>.Instance,
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task ExecuteAsync(
        IConsole console,
        IHost host,
        FileInfo? output,
        string? schemaName,
        CancellationToken cancellationToken)
    {
        var provider = host.Services.GetRequiredService<IRequestExecutorProvider>();

        if (schemaName is null)
        {
            var schemaNames = provider.SchemaNames;

            if (schemaNames.IsEmpty)
            {
                console.WriteLine("No schemas registered.");
                return;
            }

            schemaName = schemaNames.Contains(ISchemaDefinition.DefaultName)
                ? ISchemaDefinition.DefaultName
                : schemaNames[0];
        }

        var executor = await provider.GetExecutorAsync(schemaName, cancellationToken);

        var sdl = executor.Schema.ToString();

        if (output is not null)
        {
            if (Directory.Exists(output.FullName))
            {
                output = new FileInfo(System.IO.Path.Combine(output.FullName, "schema.graphqls"));
            }

            if (output.Extension is not ".graphql" and not ".graphqls")
            {
                output = new FileInfo(output.FullName + ".graphqls");
            }

            if (output.Directory?.Exists == false)
            {
                output.Directory.Create();
            }

            await File.WriteAllTextAsync(
                output.FullName,
                sdl,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true),
                cancellationToken);

            await WriteSettingsFile(output.FullName, schemaName, cancellationToken);
        }
        else
        {
            console.WriteLine(sdl);
        }
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
