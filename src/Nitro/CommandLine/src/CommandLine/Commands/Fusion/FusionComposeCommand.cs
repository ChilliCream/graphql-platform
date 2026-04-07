using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using ChilliCream.Nitro.CommandLine.Services;
using HotChocolate.Fusion;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Packaging;

namespace ChilliCream.Nitro.CommandLine.Commands.Fusion;

internal sealed class FusionComposeCommand : Command
{
    public FusionComposeCommand() : base("compose")
    {
        Description = "Compose multiple source schemas into a single composite schema.";

        Options.Add(Opt<OptionalSourceSchemaFileListOption>.Instance);
        Options.Add(Opt<OptionalFusionArchiveFileOption>.Instance);
        Options.Add(Opt<OptionalLegacyFusionArchiveFileOption>.Instance);
        Options.Add(Opt<FusionEnvironmentOption>.Instance);
        Options.Add(Opt<EnableGlobalObjectIdentificationOption>.Instance);
        Options.Add(Opt<IncludeSatisfiabilityPathsOption>.Instance);
        Options.Add(Opt<WatchModeOption>.Instance);
        Options.Add(Opt<WorkingDirectoryOption>.Instance);
        Options.Add(Opt<OptionalExcludeTagListOption>.Instance);

        this.AddGlobalNitroOptions();

        this.AddExamples(
            """
            fusion compose \
              --source-schema-file ./products/schema.graphqls \
              --source-schema-file ./reviews/schema.graphqls \
              --archive ./gateway.far \
              --env "dev"
            """);

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var fileSystem = services.GetRequiredService<IFileSystem>();
        var environmentVariables = services.GetRequiredService<IEnvironmentVariableProvider>();

        var workingDirectory = parseResult.GetValue(Opt<WorkingDirectoryOption>.Instance)
            ?? fileSystem.GetCurrentDirectory();
        var sourceSchemaFiles = parseResult.GetValue(Opt<OptionalSourceSchemaFileListOption>.Instance) ?? [];
        var archiveFile = parseResult.GetValue(Opt<OptionalFusionArchiveFileOption>.Instance);
        var environment = parseResult.GetValue(Opt<FusionEnvironmentOption>.Instance);
        var enableGlobalObjectIdentification = parseResult.GetValue(
            Opt<EnableGlobalObjectIdentificationOption>.Instance);
        var includeSatisfiabilityPaths = parseResult.GetValue(
            Opt<IncludeSatisfiabilityPathsOption>.Instance);
        var watchMode = parseResult.GetValue(Opt<WatchModeOption>.Instance);
        var tagsToExclude = parseResult.GetValue(Opt<OptionalExcludeTagListOption>.Instance);
        var legacyArchiveFile =
            parseResult.GetValue(Opt<OptionalLegacyFusionArchiveFileOption>.Instance);
        archiveFile ??= workingDirectory;

        if (fileSystem.DirectoryExists(archiveFile))
        {
            archiveFile = Path.Combine(archiveFile, "gateway.far");
        }
        else if (!Path.IsPathRooted(archiveFile))
        {
            archiveFile = Path.Combine(workingDirectory, archiveFile);
        }

        if (legacyArchiveFile is not null)
        {
            if (!Path.IsPathRooted(legacyArchiveFile))
            {
                legacyArchiveFile = Path.Combine(workingDirectory, legacyArchiveFile);
            }

            if (!fileSystem.FileExists(legacyArchiveFile))
            {
                throw new ExitException(Messages.LegacyArchiveFileDoesNotExist(legacyArchiveFile));
            }
        }

        if (sourceSchemaFiles.Count == 0)
        {
            sourceSchemaFiles.AddRange(
                fileSystem.GetFiles(workingDirectory, "*.graphql*", SearchOption.AllDirectories)
                    .Where(f => IsSchemaFile(Path.GetFileName(f))));
        }
        else
        {
            for (var i = 0; i < sourceSchemaFiles.Count; i++)
            {
                var sourceSchemaFile = sourceSchemaFiles[i];
                if (!Path.IsPathRooted(sourceSchemaFile))
                {
                    sourceSchemaFiles[i] = Path.Combine(workingDirectory, sourceSchemaFile);
                }
            }
        }

        if (watchMode)
        {
            return await WatchComposeAsync(
                console,
                fileSystem,
                environmentVariables,
                workingDirectory,
                sourceSchemaFiles,
                archiveFile,
                environment,
                enableGlobalObjectIdentification,
                includeSatisfiabilityPaths,
                tagsToExclude,
                legacyArchiveFile,
                cancellationToken);
        }

        return await ComposeAsync(
            console,
            fileSystem,
            environmentVariables,
            sourceSchemaFiles,
            archiveFile,
            environment,
            new CompositionSettings
            {
                Merger = new CompositionSettings.MergerSettings
                {
                    EnableGlobalObjectIdentification = enableGlobalObjectIdentification
                },
                Satisfiability = new CompositionSettings.SatisfiabilitySettings
                {
                    IncludeSatisfiabilityPaths = includeSatisfiabilityPaths
                },
                Preprocessor = new CompositionSettings.PreprocessorSettings
                {
                    ExcludeByTag = tagsToExclude?.ToHashSet()
                }
            },
            legacyArchiveFile,
            cancellationToken);
    }

    private static async Task<int> WatchComposeAsync(
        INitroConsole console,
        IFileSystem fileSystem,
        IEnvironmentVariableProvider environmentVariables,
        string workingDirectory,
        List<string> sourceSchemaFiles,
        string archiveFile,
        string? environment,
        bool? enableGlobalObjectIdentification,
        bool? includeSatisfiabilityPaths,
        List<string>? tagsToExclude,
        string? legacyArchiveFile,
        CancellationToken cancellationToken)
    {
        console.WriteLine("🔍 Starting watch mode...");

        // Initial composition
        await ComposeAsync(
            console,
            fileSystem,
            environmentVariables,
            sourceSchemaFiles,
            archiveFile,
            environment,
            new CompositionSettings
            {
                Merger = new CompositionSettings.MergerSettings
                {
                    EnableGlobalObjectIdentification = enableGlobalObjectIdentification
                },
                Satisfiability = new CompositionSettings.SatisfiabilitySettings
                {
                    IncludeSatisfiabilityPaths = includeSatisfiabilityPaths
                },
                Preprocessor = new CompositionSettings.PreprocessorSettings
                {
                    ExcludeByTag = tagsToExclude?.ToHashSet()
                }
            },
            legacyArchiveFile,
            cancellationToken);

        // use a bounded channel to queue composition requests
        // when already a composition is running we enqueue a new message ...
        // a single message which will trigger a new composition after the current one has completed.
        var compositionChannel = Channel.CreateBounded<string>(
            new BoundedChannelOptions(1)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = false
            });

        // start the composition processor task
        var compositionTask = ProcessCompositionRequestsAsync(
            compositionChannel.Reader,
            console,
            fileSystem,
            environmentVariables,
            sourceSchemaFiles,
            archiveFile,
            environment,
            enableGlobalObjectIdentification,
            includeSatisfiabilityPaths,
            tagsToExclude,
            legacyArchiveFile,
            cancellationToken);

        var sourceSchemaFileWatchers = new List<FileSystemWatcher>();

        foreach (var sourceSchemaPath in sourceSchemaFiles)
        {
            if (fileSystem.DirectoryExists(sourceSchemaPath))
            {
                CreateSourceSchemaWatcher(
                    sourceSchemaFileWatchers,
                    fileSystem,
                    sourceSchemaPath,
                    compositionChannel.Writer);
            }
            else if (IsSchemaFile(sourceSchemaPath))
            {
                var sourceSchemaDirectory = Path.GetDirectoryName(sourceSchemaPath)!;
                CreateSourceSchemaWatcher(
                    sourceSchemaFileWatchers,
                    fileSystem,
                    sourceSchemaDirectory,
                    compositionChannel.Writer);
            }
            else
            {
                console.Error.WriteErrorLine($"❌ The path `{sourceSchemaPath}` does not exist.");
                return 1;
            }
        }

        console.WriteLine($"👀 Watching for changes in {workingDirectory}");
        console.WriteLine("Press Ctrl+C to stop watching...");

        try
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            console.WriteLine("\n🛑 Watch mode stopped.");
        }
        finally
        {
            compositionChannel.Writer.Complete();

            foreach (var watcher in sourceSchemaFileWatchers)
            {
                watcher.Dispose();
            }

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
    }

    private static void CreateSourceSchemaWatcher(
        List<FileSystemWatcher> watchers,
        IFileSystem fileSystem,
        string sourceSchemaDirectory,
        ChannelWriter<string> writer)
    {
        var schemaFileWatcher = new FileSystemWatcher(sourceSchemaDirectory);
        schemaFileWatcher.Filter = "*.*";

        schemaFileWatcher.NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.LastWrite | NotifyFilters.FileName;
        schemaFileWatcher.Changed += (_, e) => OnSourceSchemaFileChanged(e);
        schemaFileWatcher.Created += (_, e) => OnSourceSchemaFileChanged(e);
        schemaFileWatcher.Deleted += (_, e) => OnSourceSchemaFileChanged(e);
        schemaFileWatcher.Renamed += (_, e) => OnSourceSchemaFileChanged(e);
        schemaFileWatcher.EnableRaisingEvents = true;

        watchers.Add(schemaFileWatcher);

        void OnSourceSchemaFileChanged(FileSystemEventArgs e)
        {
            var extension = Path.GetExtension(e.Name)?.ToLower();
            var fileName = Path.GetFileNameWithoutExtension(e.Name);
            var directoryName = Path.GetDirectoryName(e.Name)!;

            if (extension is ".json")
            {
                if (fileName?.EndsWith("-settings") == true)
                {
                    var schemaName = fileName[..^"-settings".Length];
                    var schemaFilePath = Path.Combine(directoryName, schemaName + ".graphql");

                    if (fileSystem.FileExists(schemaFilePath))
                    {
                        TriggerComposition($"Settings of schema `{schemaFilePath}` were modified.");
                    }
                    else if (fileSystem.FileExists(schemaFilePath + "s"))
                    {
                        TriggerComposition(
                            $"Source schema settings file {e.ChangeType.ToString().ToLower()}: {e.FullPath}");
                    }
                }
            }
            else if (extension is ".graphql" or ".graphqls")
            {
                var settingsFile = Path.Combine(directoryName, $"{fileName}-settings.json");
                if (fileSystem.FileExists(settingsFile))
                {
                    TriggerComposition($"Source schema file {e.ChangeType.ToString().ToLower()}: {e.FullPath}");
                }
            }
        }

        void TriggerComposition(string reason)
            => writer.TryWrite(reason);
    }

    private static async Task ProcessCompositionRequestsAsync(
        ChannelReader<string> reader,
        INitroConsole console,
        IFileSystem fileSystem,
        IEnvironmentVariableProvider environmentVariables,
        List<string> sourceSchemaFiles,
        string archiveFile,
        string? environment,
        bool? enableGlobalObjectIdentification,
        bool? includeSatisfiabilityPaths,
        List<string>? tagsToExclude,
        string? legacyArchiveFile,
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

                console.WriteLine($"\n🔄 {reason}");

                // Add a small delay to ensure file operations are complete
                await Task.Delay(200, cancellationToken);

                await ComposeAsync(
                    console,
                    fileSystem,
                    environmentVariables,
                    sourceSchemaFiles,
                    archiveFile,
                    environment,
                    new CompositionSettings
                    {
                        Merger = new CompositionSettings.MergerSettings
                        {
                            EnableGlobalObjectIdentification = enableGlobalObjectIdentification
                        },
                        Satisfiability = new CompositionSettings.SatisfiabilitySettings
                        {
                            IncludeSatisfiabilityPaths = includeSatisfiabilityPaths
                        },
                        Preprocessor = new CompositionSettings.PreprocessorSettings
                        {
                            ExcludeByTag = tagsToExclude?.ToHashSet()
                        }
                    },
                    legacyArchiveFile,
                    cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                console.Error.WriteErrorLine($"❌ Error during recomposition: {ex.Message}");
            }
            finally
            {
                console.WriteLine("👀 Watching for changes...");
            }
        }
    }

    public static bool IsSchemaFile(string? fileName)
    {
        if (fileName is null)
        {
            return false;
        }

        return fileName.EndsWith(".graphql", StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith(".graphqls", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<int> ComposeAsync(
        INitroConsole console,
        IFileSystem fileSystem,
        IEnvironmentVariableProvider environmentVariables,
        List<string> sourceSchemaFiles,
        string archiveFile,
        string? environment,
        CompositionSettings compositionSettings,
        string? legacyArchiveFile,
        CancellationToken cancellationToken)
    {
        using var archive = fileSystem.FileExists(archiveFile)
            ? FusionArchive.Open(archiveFile, mode: FusionArchiveMode.Update)
            : FusionArchive.Create(archiveFile);

        environment ??= environmentVariables.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        try
        {
            var sourceSchemas = await ReadSourceSchemasAsync(
                fileSystem,
                sourceSchemaFiles,
                cancellationToken);

            var compositionLog = new CompositionLog();

            Stream? legacyArchiveStream = legacyArchiveFile is not null
                ? fileSystem.OpenReadStream(legacyArchiveFile)
                : null;

            try
            {
                var result = await CompositionHelper.ComposeAsync(
                    compositionLog,
                    sourceSchemas,
                    archive,
                    environment,
                    compositionSettings,
                    legacyArchiveStream,
                    cancellationToken);

                WriteCompositionLog(
                    compositionLog,
                    console.Out,
                    writeAsGraphQLComments: false);

                if (!compositionLog.IsEmpty)
                {
                    console.Out.WriteLine();
                }

                if (result.IsFailure)
                {
                    foreach (var error in result.Errors)
                    {
                        console.Error.WriteErrorLine(error.Message);
                    }

                    return 1;
                }
            }
            finally
            {
                if (legacyArchiveStream is not null)
                {
                    await legacyArchiveStream.DisposeAsync();
                }
            }

            console.WriteLine($"✅ Composite schema written to '{archiveFile}'.");

            return 0;
        }
        catch (Exception e)
        {
            console.Error.WriteErrorLine(e.Message.EscapeMarkup());
            return 1;
        }
    }

    public static void WriteCompositionLog(
        CompositionLog compositionLog,
        IAnsiConsole output,
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
                LogSeverity.Error => "ERR",
                LogSeverity.Info => "INF",
                LogSeverity.Warning => "WRN",
                _ => throw new InvalidOperationException()
            };

            var message = $"{emoji} [{abbreviatedSeverity}] {FormatMultilineMessage(entry.Message)} ({entry.Code})";

            if (entry.ExtensionsFormatter is not null)
            {
                message +=
                    Environment.NewLine
                    + "   "
                    + FormatMultilineMessage(entry.ExtensionsFormatter.Invoke(entry.Extensions));
            }

            if (writeAsGraphQLComments)
            {
                message = $"# {message.Replace(Environment.NewLine, Environment.NewLine + "# ")}";
            }

            output.WriteLine(message);
        }
    }

    internal static async Task<Dictionary<string, (SourceSchemaText, JsonDocument)>> ReadSourceSchemasAsync(
        IFileSystem fileSystem,
        List<string> sourceSchemaFiles,
        CancellationToken cancellationToken)
    {
        var sourceSchemas = new Dictionary<string, (SourceSchemaText, JsonDocument)>();

        foreach (var sourceSchemaFile in sourceSchemaFiles)
        {
            var (schemaName, sourceText, settings) = await ReadSourceSchemaAsync(
                fileSystem,
                sourceSchemaFile,
                cancellationToken);

            sourceSchemas.Add(schemaName, (sourceText, settings));
        }

        return sourceSchemas;
    }

    internal static async Task<(string SchemaName, SourceSchemaText SourceText, JsonDocument Settings)> ReadSourceSchemaAsync(
        IFileSystem fileSystem,
        string sourceSchemaPath,
        CancellationToken cancellationToken)
    {
        string? schemaFilePath = null;

        if (fileSystem.DirectoryExists(sourceSchemaPath))
        {
            schemaFilePath =
                fileSystem
                    .GetFiles(sourceSchemaPath, "*.graphql*", SearchOption.AllDirectories)
                    .FirstOrDefault(f => IsSchemaFile(Path.GetFileName(f)));
        }
        else if (fileSystem.FileExists(sourceSchemaPath))
        {
            schemaFilePath = sourceSchemaPath;
        }

        if (schemaFilePath is null)
        {
            throw new InvalidOperationException(
                $"❌ Source schema file '{sourceSchemaPath}' does not exist.");
        }

        var settingsFilePath = Path.Combine(
            Path.GetDirectoryName(schemaFilePath)!,
            Path.GetFileNameWithoutExtension(schemaFilePath) + "-settings.json");

        if (!fileSystem.FileExists(settingsFilePath))
        {
            throw new InvalidOperationException(
                $"Missing source schema settings file `{settingsFilePath}`.");
        }

        var settings = JsonDocument.Parse(
            await fileSystem.ReadAllBytesAsync(settingsFilePath, cancellationToken));
        var schemaName = settings.RootElement.GetProperty("name").GetString();

        if (schemaName is null)
        {
            throw new InvalidOperationException("Invalid source schema settings format.");
        }

        var sourceText = await fileSystem.ReadAllTextAsync(schemaFilePath, cancellationToken);

        return (schemaName, new SourceSchemaText(schemaName, sourceText), settings);
    }

    /// <summary>
    /// Since we're prefixing the message with an emoji and space before printing,
    /// we need to also indent each line of a multiline message by three spaces to fix the alignment.
    /// </summary>
    private static string FormatMultilineMessage(string message)
    {
        var lines = message.Split(Environment.NewLine);

        if (lines.Length <= 1)
        {
            return message;
        }

        return string.Join(Environment.NewLine + "   ", lines);
    }
}
