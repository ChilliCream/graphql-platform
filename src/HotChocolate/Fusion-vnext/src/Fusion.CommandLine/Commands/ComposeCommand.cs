using System.Collections.Immutable;
using System.CommandLine;
using System.CommandLine.IO;
using System.Text;
using System.Threading.Channels;
using HotChocolate.Fusion.Logging;
using static HotChocolate.Fusion.Properties.CommandLineResources;

namespace HotChocolate.Fusion.Commands;

internal sealed class ComposeCommand : Command
{
    public ComposeCommand() : base("compose")
    {
        Description = ComposeCommand_Description;

        var workingDirectoryOption = new Option<string>("--working-directory")
        {
            Description = ComposeCommand_WorkingDirectory_Description
        };
        workingDirectoryOption.AddAlias("-w");
        workingDirectoryOption.AddValidator(result =>
        {
            var workingDirectory = result.GetValueForOption(workingDirectoryOption);

            if (!Directory.Exists(workingDirectory))
            {
                result.ErrorMessage =
                    string.Format(
                        ComposeCommand_Error_WorkingDirectoryDoesNotExist,
                        workingDirectory);
            }
        });
        workingDirectoryOption.SetDefaultValueFactory(Directory.GetCurrentDirectory);
        workingDirectoryOption.LegalFilePathsOnly();

        var sourceSchemaFileOption = new Option<List<string>>("--source-schema-file")
        {
            Description = ComposeCommand_SourceSchemaFile_Description
        };
        sourceSchemaFileOption.AddAlias("-s");
        sourceSchemaFileOption.LegalFilePathsOnly();

        var compositeSchemaFileOption = new Option<string>("--composite-schema-file")
        {
            Description = ComposeCommand_CompositeSchemaFile_Description
        };
        compositeSchemaFileOption.AddAlias("-c");
        compositeSchemaFileOption.LegalFilePathsOnly();

        var watchModeOption = new Option<bool>("--watch")
        {
            Arity = ArgumentArity.ZeroOrOne
        };

        AddOption(workingDirectoryOption);
        AddOption(sourceSchemaFileOption);
        AddOption(compositeSchemaFileOption);
        AddOption(watchModeOption);

        this.SetHandler(async context =>
        {
            var workingDirectory = context.ParseResult.GetValueForOption(workingDirectoryOption)!;
            var sourceSchemaFiles = context.ParseResult.GetValueForOption(sourceSchemaFileOption)!;
            var compositeSchemaFile = context.ParseResult.GetValueForOption(compositeSchemaFileOption);
            var watchMode = context.ParseResult.GetValueForOption(watchModeOption);

            context.ExitCode = await ExecuteAsync(
                context.Console,
                workingDirectory,
                sourceSchemaFiles,
                compositeSchemaFile,
                watchMode,
                context.GetCancellationToken());
        });
    }

    private static async Task<int> ExecuteAsync(
        IConsole console,
        string workingDirectory,
        List<string> sourceSchemaFiles,
        string? compositeSchemaFile,
        bool watchMode,
        CancellationToken cancellationToken)
    {
        if (watchMode)
        {
            return await WatchComposeAsync(
                console,
                workingDirectory,
                sourceSchemaFiles,
                compositeSchemaFile,
                cancellationToken);
        }

        return await ComposeAsync(
            console,
            workingDirectory,
            sourceSchemaFiles,
            compositeSchemaFile,
            cancellationToken);
    }

    private static async Task<int> WatchComposeAsync(
        IConsole console,
        string workingDirectory,
        List<string> sourceSchemaFiles,
        string? compositeSchemaFile,
        CancellationToken cancellationToken)
    {
        console.Out.WriteLine("🔍 Starting watch mode...");

        // Initial composition
        await ComposeAsync(console, workingDirectory, sourceSchemaFiles, compositeSchemaFile, cancellationToken);

        ImmutableSortedSet<string> sourceSchemaFilePaths;

        try
        {
            sourceSchemaFilePaths = GetSourceSchemaFilePaths(sourceSchemaFiles, workingDirectory);
        }
        catch (Exception e)
        {
            console.Error.WriteLine(e.Message);
            return 1;
        }

        using var fileWatcher = new FileSystemWatcher(workingDirectory);

        // use a bounded channel to queue composition requests
        // when already a composition is running we enqueue a new message ...
        // a single message which will trigger a new composition after the current one has completed.
        var compositionChannel = Channel.CreateBounded<string>(new BoundedChannelOptions(1)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false
        });

        // start the composition processor task
        var compositionTask = ProcessCompositionRequestsAsync(
            compositionChannel.Reader,
            console,
            workingDirectory,
            sourceSchemaFiles,
            compositeSchemaFile,
            cancellationToken);

        // set up file watcher for source schema files
        fileWatcher.Filter = "*.graphqls";
        fileWatcher.IncludeSubdirectories = true;
        fileWatcher.NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.LastWrite | NotifyFilters.FileName;

        FileSystemWatcher? gatewayFileWatcher = null;

        // Set up gateway file watcher if composite schema file is specified
        if (compositeSchemaFile is not null)
        {
            var compositeSchemaPath = Path.Combine(workingDirectory, compositeSchemaFile);
            var gatewayDirectory = Path.GetDirectoryName(compositeSchemaPath);
            var gatewayFileName = Path.GetFileName(compositeSchemaPath);

            if (gatewayDirectory is not null)
            {
                gatewayFileWatcher = new FileSystemWatcher(gatewayDirectory);
                gatewayFileWatcher.Filter = gatewayFileName;
                gatewayFileWatcher.NotifyFilter = NotifyFilters.FileName;

                gatewayFileWatcher.Deleted += (_, e) => OnGatewayFileDeleted(e);
                gatewayFileWatcher.EnableRaisingEvents = true;
            }
        }

        fileWatcher.Changed += (_, e) => OnSourceSchemaFileChanged(e);
        fileWatcher.Created += (_, e) => OnSourceSchemaFileChanged(e);
        fileWatcher.Deleted += (_, e) => OnSourceSchemaFileChanged(e);
        fileWatcher.Renamed += (_, e) => OnSourceSchemaFileChanged(e);

        fileWatcher.EnableRaisingEvents = true;

        console.Out.WriteLine($"👀 Watching for changes in {workingDirectory}");
        console.Out.WriteLine("Press Ctrl+C to stop watching...");

        try
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            console.Out.WriteLine("\n🛑 Watch mode stopped.");
        }
        finally
        {
            compositionChannel.Writer.Complete();
            gatewayFileWatcher?.Dispose();

            try
            {
                await compositionTask;
            }
            catch (OperationCanceledException)
            {
                // expected when cancellation is requested
            }
        }

        return 0;

        void OnSourceSchemaFileChanged(FileSystemEventArgs e)
        {
            // only process files that are in our source schema paths or match the pattern
            var isRelevantFile = sourceSchemaFilePaths.Any(path =>
                string.Equals(path, e.FullPath, StringComparison.OrdinalIgnoreCase))
                || e.Name?.EndsWith(".graphqls", StringComparison.OrdinalIgnoreCase) == true;

            if (!isRelevantFile)
            {
                return;
            }

            TriggerComposition($"Source schema file {e.ChangeType.ToString().ToLower()}: {e.Name}");
        }

        void OnGatewayFileDeleted(FileSystemEventArgs e)
            => TriggerComposition($"Gateway file deleted: {e.Name}");

        void TriggerComposition(string reason)
            => compositionChannel.Writer.TryWrite(reason);
    }

    private static async Task ProcessCompositionRequestsAsync(
        ChannelReader<string> reader,
        IConsole console,
        string workingDirectory,
        List<string> sourceSchemaFiles,
        string? compositeSchemaFile,
        CancellationToken cancellationToken)
    {
        var lastComposition = DateTime.MinValue;

        await foreach (var reason in reader.ReadAllAsync(cancellationToken))
        {
            try
            {
                // Debounce rapid file changes (wait at least 500ms between compositions)
                var now = DateTime.UtcNow;
                var timeSinceLastComposition = now - lastComposition;

                if (timeSinceLastComposition.TotalMilliseconds < 500)
                {
                    var delayTime = 500 - (int)timeSinceLastComposition.TotalMilliseconds;
                    await Task.Delay(delayTime, cancellationToken);
                }

                lastComposition = DateTime.UtcNow;

                console.Out.WriteLine($"\n🔄 {reason}");
                console.Out.WriteLine("Recomposing schema...");

                // Add a small delay to ensure file operations are complete
                await Task.Delay(100, cancellationToken);

                await ComposeAsync(console, workingDirectory, sourceSchemaFiles, compositeSchemaFile, cancellationToken);

                console.Out.WriteLine("✅ Recomposition complete");
                console.Out.WriteLine("👀 Watching for changes...");
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                console.Error.WriteLine($"❌ Error during recomposition: {ex.Message}");
                console.Out.WriteLine("👀 Watching for changes...");
            }
        }
    }

    private static ImmutableSortedSet<string> GetSourceSchemaFilePaths(
        List<string> sourceSchemaFiles,
        string workingDirectory)
    {
        // If no source schema files were specified, scan the working directory for *.graphqls files
        if (sourceSchemaFiles.Count > 0)
        {
            return sourceSchemaFiles
                .Select(f => Path.Combine(workingDirectory, f))
                .ToImmutableSortedSet();
        }

        var foundFiles = new DirectoryInfo(workingDirectory)
            .GetFiles("*.graphqls", SearchOption.AllDirectories)
            .Select(i => i.FullName)
            .ToImmutableSortedSet();

        if (foundFiles.Count == 0)
        {
            throw new Exception(ComposeCommand_Error_NoSourceSchemaFilesFound);
        }

        return foundFiles;
    }

    private static async Task<int> ComposeAsync(
        IConsole console,
        string workingDirectory,
        List<string> sourceSchemaFiles,
        string? compositeSchemaFile,
        CancellationToken cancellationToken)
    {
        IEnumerable<string> sourceSchemas;

        try
        {
            sourceSchemas = await ReadSourceSchemasAsync(
                sourceSchemaFiles,
                workingDirectory,
                cancellationToken);
        }
        catch (Exception e)
        {
            console.Error.WriteLine(e.Message);

            return 1;
        }

        var compositionLog = new CompositionLog();
        var schemaComposer = new SchemaComposer(sourceSchemas, compositionLog);

        var result = schemaComposer.Compose();

        WriteCompositionLog(
            compositionLog,
            writer: result.IsSuccess ? console.Out : console.Error,
            writeAsGraphQLComments: result.IsSuccess && compositeSchemaFile is null);

        if (result.IsFailure)
        {
            foreach (var error in result.Errors)
            {
                console.Error.WriteLine(error.Message);
            }

            return 1;
        }

        // If a composite schema file was not specified, print the result to the console.
        if (compositeSchemaFile is null)
        {
            console.Out.WriteLine(result.Value.ToString());
        }
        else
        {
            var compositeSchemaPath = Path.Combine(workingDirectory, compositeSchemaFile);
            var directoryPath = Path.GetDirectoryName(compositeSchemaPath);

            if (directoryPath is not null && !Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            await File.WriteAllTextAsync(
                compositeSchemaPath,
                result.Value + Environment.NewLine,
                cancellationToken);

            console.Out.WriteLine(
                string.Format(ComposeCommand_CompositeSchemaFile_Written, compositeSchemaPath));
        }

        return 0;
    }

    private static void WriteCompositionLog(
        CompositionLog compositionLog,
        IStandardStreamWriter writer,
        bool writeAsGraphQLComments)
    {
        Console.OutputEncoding = Encoding.UTF8;

        foreach (var entry in compositionLog)
        {
            var emoji = entry.Severity switch
            {
                LogSeverity.Error => "❌",
                LogSeverity.Info => "ℹ️",
                LogSeverity.Warning => "⚠️",
                _ => throw new InvalidOperationException()
            };

            var abbreviatedSeverity = entry.Severity switch
            {
                LogSeverity.Error => ComposeCommand_AbbreviatedSeverity_Error,
                LogSeverity.Info => ComposeCommand_AbbreviatedSeverity_Info,
                LogSeverity.Warning => ComposeCommand_AbbreviatedSeverity_Warning,
                _ => throw new InvalidOperationException()
            };

            var message = $"{emoji} [{abbreviatedSeverity}] {entry.Message} ({entry.Code})";

            if (writeAsGraphQLComments)
            {
                message = $"# {message}";
            }

            writer.WriteLine(message);
        }

        if (!compositionLog.IsEmpty)
        {
            writer.WriteLine();
        }
    }

    private static async Task<IEnumerable<string>> ReadSourceSchemasAsync(
        List<string> sourceSchemaFiles,
        string workingDirectory,
        CancellationToken cancellationToken)
    {
        ImmutableSortedSet<string> sourceSchemaFilePaths;

        // If no source schema files were specified, scan the working directory for *.graphqls
        // files.
        if (sourceSchemaFiles.Count == 0)
        {
            sourceSchemaFilePaths =
                new DirectoryInfo(workingDirectory)
                    .GetFiles("*.graphqls")
                    .Select(i => i.FullName)
                    .ToImmutableSortedSet();

            if (sourceSchemaFilePaths.Count == 0)
            {
                throw new Exception(ComposeCommand_Error_NoSourceSchemaFilesFound);
            }
        }
        else
        {
            sourceSchemaFilePaths
                = sourceSchemaFiles.Select(f => Path.Combine(workingDirectory, f))
                    .ToImmutableSortedSet();
        }

        foreach (var sourceSchemaFilePath in sourceSchemaFilePaths)
        {
            if (!File.Exists(sourceSchemaFilePath))
            {
                throw new Exception(
                    string.Format(
                        ComposeCommand_Error_SourceSchemaFileDoesNotExist,
                        sourceSchemaFilePath));
            }
        }

        return await Task.WhenAll(
            sourceSchemaFilePaths.Select(
                async f => await File.ReadAllTextAsync(f, cancellationToken)));
    }
}
