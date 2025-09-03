using System.CommandLine;
using System.CommandLine.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using HotChocolate.Buffers;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Logging.Contracts;
using HotChocolate.Fusion.Options;
using HotChocolate.Fusion.Packaging;
using HotChocolate.Fusion.Results;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Properties.CommandLineResources;

namespace HotChocolate.Fusion.CommandLine;

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

        var archiveOption = new Option<string>("--fusion-archive")
        {
            Description = ComposeCommand_CompositeSchemaFile_Description
        };
        archiveOption.AddAlias("--far");
        archiveOption.AddAlias("-f");
        archiveOption.LegalFilePathsOnly();

        var environmentOption = new Option<string?>("--environment");
        environmentOption.AddAlias("--env");
        environmentOption.AddAlias("-e");

        var enableGlobalIdsOption = new Option<bool>("--enable-global-object-identification")
        {
            Description = ComposeCommand_EnableGlobalObjectIdentification_Description
        };

        var watchModeOption = new Option<bool>("--watch") { Arity = ArgumentArity.ZeroOrOne };

        var printSchemaOption = new Option<bool>("--print") { IsHidden = true };

        AddOption(workingDirectoryOption);
        AddOption(sourceSchemaFileOption);
        AddOption(archiveOption);
        AddOption(environmentOption);
        AddOption(enableGlobalIdsOption);
        AddOption(watchModeOption);
        AddOption(printSchemaOption);

        this.SetHandler(async context =>
        {
            var workingDirectory = context.ParseResult.GetValueForOption(workingDirectoryOption)!;
            var sourceSchemaFiles = context.ParseResult.GetValueForOption(sourceSchemaFileOption)!;
            var archive = context.ParseResult.GetValueForOption(archiveOption);
            var environment = context.ParseResult.GetValueForOption(environmentOption);
            var enableGlobalIds = context.ParseResult.GetValueForOption(enableGlobalIdsOption);
            var watchMode = context.ParseResult.GetValueForOption(watchModeOption);
            var printSchema = context.ParseResult.GetValueForOption(printSchemaOption);

            context.ExitCode = await ExecuteAsync(
                context.Console,
                workingDirectory,
                sourceSchemaFiles,
                archive,
                environment,
                enableGlobalIds,
                watchMode,
                printSchema,
                context.GetCancellationToken());
        });
    }

    private static async Task<int> ExecuteAsync(
        IConsole console,
        string workingDirectory,
        List<string> sourceSchemaFiles,
        string? archiveFile,
        string? environment,
        bool enableGlobalObjectIdentification,
        bool watchMode,
        bool printSchema,
        CancellationToken cancellationToken)
    {
        archiveFile ??= workingDirectory;

        if (Directory.Exists(archiveFile))
        {
            archiveFile = Path.Combine(archiveFile, "gateway.far");
        }
        else if (!Path.IsPathRooted(archiveFile))
        {
            archiveFile = Path.Combine(workingDirectory, archiveFile);
        }

        if (sourceSchemaFiles.Count == 0)
        {
            sourceSchemaFiles.AddRange(
                new DirectoryInfo(workingDirectory)
                    .GetFiles("*.graphql*", SearchOption.AllDirectories)
                    .Where(f => IsSchemaFile(f.Name))
                    .Select(i => i.FullName));
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
                workingDirectory,
                sourceSchemaFiles,
                archiveFile,
                environment,
                enableGlobalObjectIdentification,
                cancellationToken);
        }

        return await ComposeAsync(
            console,
            sourceSchemaFiles,
            archiveFile,
            environment,
            enableGlobalObjectIdentification,
            printSchema,
            cancellationToken);
    }

    private static async Task<int> WatchComposeAsync(
        IConsole console,
        string workingDirectory,
        List<string> sourceSchemaFiles,
        string archiveFile,
        string? environment,
        bool enableGlobalObjectIdentification,
        CancellationToken cancellationToken)
    {
        console.Out.WriteLine("🔍 Starting watch mode...");

        // Initial composition
        await ComposeAsync(
            console,
            sourceSchemaFiles,
            archiveFile,
            environment,
            enableGlobalObjectIdentification,
            false,
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
            sourceSchemaFiles,
            archiveFile,
            environment,
            enableGlobalObjectIdentification,
            cancellationToken);

        var sourceSchemaFileWatchers = new List<FileSystemWatcher>();

        foreach (var sourceSchemaPath in sourceSchemaFiles)
        {
            if (Directory.Exists(sourceSchemaPath))
            {
                CreateSourceSchemaWatcher(sourceSchemaFileWatchers, sourceSchemaPath, compositionChannel.Writer);
            }
            else if (IsSchemaFile(sourceSchemaPath))
            {
                var sourceSchemaDirectory = Path.GetDirectoryName(sourceSchemaPath)!;
                CreateSourceSchemaWatcher(sourceSchemaFileWatchers, sourceSchemaDirectory, compositionChannel.Writer);
            }
            else
            {
                console.Out.WriteLine($"❌ The path `{sourceSchemaPath}` does not exist.");
                return 1;
            }
        }

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

                    if (File.Exists(schemaFilePath))
                    {
                        TriggerComposition($"Settings of schema `{schemaFilePath}` were modified.");
                    }
                    else if (File.Exists(schemaFilePath + "s"))
                    {
                        TriggerComposition(
                            $"Source schema settings file {e.ChangeType.ToString().ToLower()}: {e.FullPath}");
                    }
                }
            }
            else if (extension is ".graphql" or ".graphqls")
            {
                var settingsFile = Path.Combine(directoryName, $"{fileName}-settings.json");
                if (File.Exists(settingsFile))
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
        IConsole console,
        List<string> sourceSchemaFiles,
        string archiveFile,
        string? environment,
        bool enableGlobalObjectIdentification,
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

                // Add a small delay to ensure file operations are complete
                await Task.Delay(200, cancellationToken);

                await ComposeAsync(
                    console,
                    sourceSchemaFiles,
                    archiveFile,
                    environment,
                    enableGlobalObjectIdentification,
                    false,
                    cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                console.Error.WriteLine($"❌ Error during recomposition: {ex.Message}");
            }
            finally
            {
                console.Out.WriteLine("👀 Watching for changes...");
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
        IConsole console,
        List<string> sourceSchemaFiles,
        string archiveFile,
        string? environment,
        bool enableGlobalObjectIdentification,
        bool printSchema,
        CancellationToken cancellationToken)
    {
        using var archive = File.Exists(archiveFile)
            ? FusionArchive.Open(archiveFile, mode: FusionArchiveMode.Update)
            : FusionArchive.Create(archiveFile);

        try
        {
            var compositionLog = new CompositionLog();

            var result = await ComposeAsync(
                compositionLog,
                sourceSchemaFiles,
                archive,
                environment,
                enableGlobalObjectIdentification,
                cancellationToken);

            WriteCompositionLog(
                compositionLog,
                writer: result.IsSuccess ? console.Out : console.Error,
                writeAsGraphQLComments: result.IsSuccess && printSchema);

            if (result.IsFailure)
            {
                foreach (var error in result.Errors)
                {
                    console.Error.WriteLine(error.Message);
                }

                return 1;
            }

            console.Out.WriteLine(printSchema
                ? result.Value.ToString()
                : string.Format(ComposeCommand_CompositeSchemaFile_Written, archiveFile));

            return 0;
        }
        catch (Exception e)
        {
            console.Error.WriteLine(e.Message);
            return 1;
        }
    }

    public static async Task<CompositionResult<MutableSchemaDefinition>> ComposeAsync(
        ICompositionLog compositionLog,
        List<string> sourceSchemaFiles,
        FusionArchive archive,
        string? environment,
        bool enableGlobalObjectIdentification,
        CancellationToken cancellationToken)
    {
        environment ??= Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        var sourceSchemas = await ReadSourceSchemasAsync(
            sourceSchemaFiles,
            cancellationToken);

        var sourceSchemaNamesInPackage = new SortedSet<string>(
            await archive.GetSourceSchemaNamesAsync(cancellationToken),
            StringComparer.Ordinal);

        foreach (var schemaName in sourceSchemaNamesInPackage)
        {
            if (sourceSchemas.ContainsKey(schemaName))
            {
                // We have a new configuration for the schema, so we'll take that
                // instead of the one in the gateway package.
                continue;
            }

            var configuration = await archive.TryGetSourceSchemaConfigurationAsync(schemaName, cancellationToken);

            if (configuration is null)
            {
                continue;
            }

            var sourceText = await ReadSchemaSourceTextAsync(configuration, cancellationToken);

            sourceSchemas[schemaName] = (new SourceSchemaText(schemaName, sourceText), configuration.Settings);
        }

        var schemaComposerOptions = new SchemaComposerOptions
        {
            EnableGlobalObjectIdentification = enableGlobalObjectIdentification
        };

        var schemaComposer = new SchemaComposer(
            sourceSchemas.Values.Select(t => t.Item1),
            schemaComposerOptions,
            compositionLog);

        var result = schemaComposer.Compose();

        if (result.IsFailure)
        {
            return result;
        }

        using var bufferWriter = new PooledArrayWriter();
        new SettingsComposer().Compose(
            bufferWriter,
            sourceSchemas.Values.Select(t => t.Item2.RootElement).ToArray(),
            environment);

        var metadata = new ArchiveMetadata
        {
            SupportedGatewayFormats = [new Version(2, 0, 0)],
            SourceSchemas = [..sourceSchemas.Keys]
        };

        await archive.SetArchiveMetadataAsync(metadata, cancellationToken);

        foreach (var (schemaName, (schema, settings)) in sourceSchemas)
        {
            await archive.SetSourceSchemaConfigurationAsync(
                schemaName,
                Encoding.UTF8.GetBytes(schema.SourceText),
                settings,
                cancellationToken);
        }

        await archive.SetGatewayConfigurationAsync(
            result.Value + Environment.NewLine,
            JsonDocument.Parse(bufferWriter.WrittenMemory),
            new Version(2, 0, 0),
            cancellationToken);

        await archive.CommitAsync(cancellationToken);

        return result;
    }

    public static void WriteCompositionLog(
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

            var message = $"{emoji} [{abbreviatedSeverity}] {FormatMultilineMessage(entry.Message)} ({entry.Code})";

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

    private static async Task<Dictionary<string, (SourceSchemaText, JsonDocument)>> ReadSourceSchemasAsync(
        List<string> sourceSchemaFiles,
        CancellationToken cancellationToken)
    {
        var sourceSchemas = new Dictionary<string, (SourceSchemaText, JsonDocument)>();

        foreach (var sourceSchemaFile in sourceSchemaFiles)
        {
            await ReadSourceSchemaAsync(sourceSchemaFile, sourceSchemas, cancellationToken);
        }

        return sourceSchemas;

        static async Task ReadSourceSchemaAsync(
            string sourceSchemaPath,
            Dictionary<string, (SourceSchemaText, JsonDocument)> sourceSchemas,
            CancellationToken cancellationToken)
        {
            string? schemaFilePath = null;

            if (Directory.Exists(sourceSchemaPath))
            {
                schemaFilePath =
                    new DirectoryInfo(sourceSchemaPath)
                        .GetFiles("*.graphql*", SearchOption.AllDirectories)
                        .Where(f => IsSchemaFile(f.Name))
                        .Select(i => i.FullName)
                        .FirstOrDefault();
            }
            else if (File.Exists(sourceSchemaPath))
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

            if (!File.Exists(settingsFilePath))
            {
                throw new InvalidOperationException(
                    $"Missing source schema settings file `{settingsFilePath}`.");
            }

            var settings = JsonDocument.Parse(await File.ReadAllBytesAsync(settingsFilePath, cancellationToken));
            var schemaName = settings.RootElement.GetProperty("name").GetString();

            if (schemaName is null)
            {
                throw new InvalidOperationException("Invalid source schema settings format.");
            }

            var sourceText = await File.ReadAllTextAsync(schemaFilePath, cancellationToken);
            sourceSchemas.TryAdd(schemaName, (new SourceSchemaText(schemaName, sourceText), settings));
        }
    }

    private static async Task<string> ReadSchemaSourceTextAsync(
        SourceSchemaConfiguration configuration,
        CancellationToken cancellationToken)
    {
        await using var stream = await configuration.OpenReadSchemaAsync(cancellationToken);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return await reader.ReadToEndAsync(cancellationToken);
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
