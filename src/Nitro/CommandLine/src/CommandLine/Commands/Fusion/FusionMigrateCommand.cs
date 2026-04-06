using System.Text.Json;
using ChilliCream.Nitro.CommandLine.Arguments;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;

namespace ChilliCream.Nitro.CommandLine.Commands.Fusion;

internal sealed class FusionMigrateCommand : Command
{
    public FusionMigrateCommand() : base("migrate")
    {
        Description = "Migrate Fusion configuration files.";

        Arguments.Add(Opt<FusionMigrateTargetArgument>.Instance);
        Options.Add(Opt<WorkingDirectoryOption>.Instance);

        this.AddExamples("fusion migrate subgraph-config");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var fileSystem = services.GetRequiredService<IFileSystem>();

        var target = parseResult.GetValue(Opt<FusionMigrateTargetArgument>.Instance);
        var workingDirectory = parseResult.GetValue(Opt<WorkingDirectoryOption>.Instance)
            ?? fileSystem.GetCurrentDirectory();

        return target switch
        {
            FusionMigrateTargetArgument.SubgraphConfig => await MigrateSubgraphConfigAsync(
                console,
                fileSystem,
                workingDirectory,
                cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(target))
        };
    }

    private static async Task<int> MigrateSubgraphConfigAsync(
        INitroConsole console,
        IFileSystem fileSystem,
        string workingDirectory,
        CancellationToken cancellationToken)
    {
        const string sourceFileName = "subgraph-config.json";
        const string targetFileName = "schema-settings.json";

        console.WriteLine($"Searching for '{sourceFileName}' files in '{workingDirectory}'...");

        var sourceFiles = fileSystem.GlobMatch(
            [$"{workingDirectory}/**/{sourceFileName}"],
            ["**/bin/**", "**/obj/**"])
            .ToArray();

        if (sourceFiles.Length == 0)
        {
            console.Error.WriteErrorLine($"Could not find any '{sourceFileName}' files in '{workingDirectory}'.");
            return ExitCodes.Error;
        }

        var migratedFiles = new List<string>();

        foreach (var sourceFile in sourceFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var directory = Path.GetDirectoryName(sourceFile)!;
            var targetFile = Path.Combine(directory, targetFileName);

            if (fileSystem.FileExists(targetFile))
            {
                var relativePath = Path.GetRelativePath(workingDirectory, targetFile);
                console.MarkupLineInterpolated(
                    $"[yellow]Skipping[/] [grey]{relativePath}[/] (already exists)");
                continue;
            }

            var sourceJson = await fileSystem.ReadAllBytesAsync(sourceFile, cancellationToken);

            using var document = FusionMigrationHelpers.MigrateSubgraphConfig(sourceJson);
            var root = document.RootElement;

            if (root.TryGetProperty("name", out var nameElement)
                && nameElement.GetString() is "")
            {
                var relativePath = Path.GetRelativePath(workingDirectory, targetFile);
                console.MarkupLineInterpolated(
                    $"[grey]{relativePath}[/] [yellow]needs to define a 'name'.[/]");
            }

            await using var stream = fileSystem.CreateFile(targetFile);
            await using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions
            {
                Indented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });

            root.WriteTo(writer);
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
}
