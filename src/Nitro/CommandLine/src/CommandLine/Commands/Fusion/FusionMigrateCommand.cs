using System.Text.Json;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;

namespace ChilliCream.Nitro.CommandLine.Commands.Fusion;

internal sealed class FusionMigrateCommand : Command
{
    public FusionMigrateCommand() : base("migrate")
    {
        Description = "Migrate Fusion configuration files";

        var targetArgument = new Argument<string>("TARGET")
            .FromAmong(Targets.SubgraphConfig);

        AddArgument(targetArgument);
        AddOption(Opt<WorkingDirectoryOption>.Instance);

        this.SetHandler(async context =>
        {
            var target = context.ParseResult.GetValueForArgument(targetArgument);
            var workingDirectory = context.ParseResult.GetValueForOption(Opt<WorkingDirectoryOption>.Instance)!;
            var console = context.BindingContext.GetRequiredService<IAnsiConsole>();

            context.ExitCode = target switch
            {
                Targets.SubgraphConfig => await MigrateSubgraphConfigAsync(
                    console,
                    workingDirectory,
                    context.GetCancellationToken()),
                _ => throw new ArgumentOutOfRangeException(nameof(target))
            };
        });
    }

    private static async Task<int> MigrateSubgraphConfigAsync(
        IAnsiConsole console,
        string workingDirectory,
        CancellationToken cancellationToken)
    {
        const string sourceFileName = "subgraph-config.json";
        const string targetFileName = "schema-settings.json";

        console.WriteLine($"Searching for '{sourceFileName}' files in '{workingDirectory}'...");

        var sourceFiles = Directory.GetFiles(
            workingDirectory,
            sourceFileName,
            SearchOption.AllDirectories);

        if (sourceFiles.Length == 0)
        {
            console.ErrorLine($"No {sourceFileName} files found.");
            return ExitCodes.Error;
        }

        var migratedFiles = new List<string>();

        foreach (var sourceFile in sourceFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var directory = Path.GetDirectoryName(sourceFile)!;
            var targetFile = Path.Combine(directory, targetFileName);

            if (File.Exists(targetFile))
            {
                var relativePath = Path.GetRelativePath(workingDirectory, targetFile);
                console.MarkupLineInterpolated(
                    $"[yellow]Skipping[/] [grey]{relativePath}[/] (already exists)");
                continue;
            }

            var sourceJson = await File.ReadAllBytesAsync(sourceFile, cancellationToken);

            using var document = JsonDocument.Parse(sourceJson);
            var root = document.RootElement;

            await using var stream = File.Create(targetFile);
            await using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions
            {
                Indented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });

            writer.WriteStartObject();

            // Enable backwards compatibility
            writer.WriteString("version", "1.0.0");

            // "subgraph" -> "name"
            if (root.TryGetProperty("subgraph", out var subgraphElement))
            {
                writer.WritePropertyName("name");
                subgraphElement.WriteTo(writer);
            }
            else
            {
                writer.WriteString("name", "");

                var relativePath = Path.GetRelativePath(workingDirectory, targetFile);
                console.MarkupLineInterpolated(
                    $"[grey]{relativePath}[/] [yellow]needs to define a 'name'.[/]");
            }

            // "http" -> "transports.http" with "baseAddress" -> "url"
            if (root.TryGetProperty("http", out var httpElement))
            {
                writer.WriteStartObject("transports");
                writer.WriteStartObject("http");

                foreach (var httpProperty in httpElement.EnumerateObject())
                {
                    if (httpProperty.NameEquals("baseAddress"))
                    {
                        writer.WritePropertyName("url");
                        httpProperty.Value.WriteTo(writer);
                    }
                    else
                    {
                        httpProperty.WriteTo(writer);
                    }
                }

                writer.WriteEndObject();
                writer.WriteEndObject();
            }

            // Copy any other top-level properties except "subgraph", "http", and "websocket"
            foreach (var property in root.EnumerateObject())
            {
                if (property.NameEquals("subgraph")
                    || property.NameEquals("http")
                    || property.NameEquals("websocket"))
                {
                    continue;
                }

                property.WriteTo(writer);
            }

            writer.WriteEndObject();
            await writer.FlushAsync(cancellationToken);

            migratedFiles.Add(sourceFile);
        }

        if (migratedFiles.Count == 0)
        {
            console.MarkupLine("[yellow]No files were migrated.[/]");
            return ExitCodes.Success;
        }

        console.Success($"Migrated {migratedFiles.Count} file(s) to {targetFileName}!");

        foreach (var sourceFile in migratedFiles)
        {
            var relativePath = Path.GetRelativePath(workingDirectory, sourceFile);
            console.MarkupLineInterpolated(
                $"[grey]{relativePath}[/] -> [green]{targetFileName}[/]");
        }

        return ExitCodes.Success;
    }

    private static class Targets
    {
        public const string SubgraphConfig = "subgraph-config";
    }
}
