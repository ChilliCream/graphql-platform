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
        Options.Add(Opt<OptionalSourceSchemaUrlOption>.Instance);
        Options.Add(Opt<OptionalSourceSchemaSettingsFileOption>.Instance);
        Options.Add(Opt<OptionalFusionArchiveFileOption>.Instance);
        Options.Add(Opt<FusionEnvironmentOption>.Instance);
        Options.Add(Opt<CacheControlMergeBehaviorOption>.Instance);
        Options.Add(Opt<EnableGlobalObjectIdentificationOption>.Instance);
        Options.Add(Opt<NodeResolutionOption>.Instance);
        Options.Add(Opt<TagMergeBehaviorOption>.Instance);
        Options.Add(Opt<ShareableFieldRuntimeTypeRoutingOption>.Instance);
        Options.Add(Opt<AllowNonResolvableInterfaceObjectsOption>.Instance);
        Options.Add(Opt<IncludeSatisfiabilityPathsOption>.Instance);
        Options.Add(Opt<WatchModeOption>.Instance);
        Options.Add(Opt<WorkingDirectoryOption>.Instance);
        Options.Add(Opt<OptionalExcludeTagListOption>.Instance);
        Options.Add(Opt<OptionalRemoveSourceSchemaListOption>.Instance);

        this.AddGlobalNitroOptions();

        Validators.Add(result =>
        {
            var removeSourceSchemas =
                result.GetValue(Opt<OptionalRemoveSourceSchemaListOption>.Instance);
            var watchMode = result.GetValue(Opt<WatchModeOption>.Instance);

            if (removeSourceSchemas is { Count: > 0 } && watchMode)
            {
                result.AddError(
                    "The '--remove-source-schema' and '--watch' options cannot be combined.");
            }
        });

        this.AddExamples(
            """
            fusion compose \
              --source-schema-file ./products/schema.graphqls \
              --source-schema-url https://reviews.example.com/graphql \
              --source-schema-settings-file ./reviews/schema-settings.json \
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
        var sourceSchemaUrlValues = parseResult
            .GetResult(Opt<OptionalSourceSchemaUrlOption>.Instance)?
            .Tokens
            .Select(token => token.Value)
            .ToList() ?? [];
        var sourceSchemaSettingsFiles = parseResult
            .GetResult(Opt<OptionalSourceSchemaSettingsFileOption>.Instance)?
            .Tokens
            .Select(token => token.Value)
            .ToList() ?? [];
        var archiveFile = parseResult.GetValue(Opt<OptionalFusionArchiveFileOption>.Instance);
        var environment = parseResult.GetValue(Opt<FusionEnvironmentOption>.Instance);
        var cacheControlMergeBehaviorOption = Opt<CacheControlMergeBehaviorOption>.Instance;
        var cacheControlMergeBehavior = parseResult.Tokens.Any(
            static token => token.Value == CacheControlMergeBehaviorOption.OptionName)
            ? parseResult.GetValue(cacheControlMergeBehaviorOption)
            : null;
        var enableGlobalObjectIdentification = parseResult.GetValue(
            Opt<EnableGlobalObjectIdentificationOption>.Instance);
        var nodeResolutionOption = Opt<NodeResolutionOption>.Instance;
        var nodeResolution = parseResult.Tokens.Any(
            static token => token.Value == NodeResolutionOption.OptionName)
            ? parseResult.GetValue(nodeResolutionOption)
            : null;
        var tagMergeBehaviorOption = Opt<TagMergeBehaviorOption>.Instance;
        var tagMergeBehavior = parseResult.Tokens.Any(
            static token => token.Value == TagMergeBehaviorOption.OptionName)
            ? parseResult.GetValue(tagMergeBehaviorOption)
            : null;
        var shareableFieldRuntimeTypeRoutingOption =
            Opt<ShareableFieldRuntimeTypeRoutingOption>.Instance;
        var shareableFieldRuntimeTypeRouting = parseResult.Tokens.Any(
            static token => token.Value == ShareableFieldRuntimeTypeRoutingOption.OptionName)
            ? parseResult.GetValue(shareableFieldRuntimeTypeRoutingOption)
            : null;
        var allowNonResolvableInterfaceObjects = parseResult.GetValue(
            Opt<AllowNonResolvableInterfaceObjectsOption>.Instance);
        var includeSatisfiabilityPaths = parseResult.GetValue(
            Opt<IncludeSatisfiabilityPathsOption>.Instance);
        var watchMode = parseResult.GetValue(Opt<WatchModeOption>.Instance);
        var tagsToExclude = parseResult.GetValue(Opt<OptionalExcludeTagListOption>.Instance);
        var removeSourceSchemas = parseResult.GetValue(Opt<OptionalRemoveSourceSchemaListOption>.Instance) ?? [];
        archiveFile ??= workingDirectory;

        var remoteSourceSchemaInputs = new List<RemoteSourceSchemaInput>(
            sourceSchemaUrlValues.Count);

        if (sourceSchemaUrlValues.Count != sourceSchemaSettingsFiles.Count)
        {
            throw new ExitException(Messages.SourceSchemaUrlSettingsCountMismatch());
        }

        for (var i = 0; i < sourceSchemaUrlValues.Count; i++)
        {
            var url = sourceSchemaUrlValues[i];
            if (!Uri.TryCreate(url, UriKind.Absolute, out var endpoint)
                || endpoint.Scheme is not ("http" or "https")
                || !string.IsNullOrEmpty(endpoint.UserInfo)
                || !string.IsNullOrEmpty(endpoint.Fragment))
            {
                throw new ExitException(Messages.SourceSchemaUrlInvalid());
            }

            var settingsFile = sourceSchemaSettingsFiles[i];
            if (!Path.IsPathRooted(settingsFile))
            {
                settingsFile = Path.Combine(workingDirectory, settingsFile);
            }

            remoteSourceSchemaInputs.Add(new(endpoint, settingsFile));
        }

        var compositionSettings = new CompositionSettings
        {
            Merger = new CompositionSettings.MergerSettings
            {
                CacheControlMergeBehavior = cacheControlMergeBehavior,
                EnableGlobalObjectIdentification = enableGlobalObjectIdentification,
                NodeResolution = nodeResolution,
                TagMergeBehavior = tagMergeBehavior
            },
            Satisfiability = new CompositionSettings.SatisfiabilitySettings
            {
                IncludeSatisfiabilityPaths = includeSatisfiabilityPaths
            },
            Preprocessor = new CompositionSettings.PreprocessorSettings
            {
                ExcludeByTag = tagsToExclude?.ToHashSet()
            },
            ApolloFederationCompatibility =
                new CompositionSettings.ApolloFederationCompatibilitySettings
                {
                    AllowNonResolvableInterfaceObjects =
                        allowNonResolvableInterfaceObjects,
                    ShareableFieldRuntimeTypeRouting = shareableFieldRuntimeTypeRouting
                }
        };

        if (fileSystem.DirectoryExists(archiveFile))
        {
            archiveFile = Path.Combine(archiveFile, "gateway.far");
        }
        else if (!Path.IsPathRooted(archiveFile))
        {
            archiveFile = Path.Combine(workingDirectory, archiveFile);
        }

        if (sourceSchemaFiles.Count == 0
            && remoteSourceSchemaInputs.Count == 0
            && removeSourceSchemas.Count == 0)
        {
            sourceSchemaFiles.AddRange(
                fileSystem.GetFiles(workingDirectory, "*.graphql*", SearchOption.AllDirectories)
                    .Where(f => FusionCompositionHelpers.IsSchemaFile(Path.GetFileName(f))));
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

        using var httpClient = remoteSourceSchemaInputs.Count > 0
            ? services.GetRequiredService<IHttpClientFactory>()
                .CreateClient("fusion-composition")
            : null;

        if (watchMode)
        {
            return await WatchComposeAsync(
                console,
                fileSystem,
                environmentVariables,
                workingDirectory,
                sourceSchemaFiles,
                remoteSourceSchemaInputs,
                archiveFile,
                environment,
                compositionSettings,
                httpClient,
                cancellationToken);
        }

        return await ComposeAsync(
            console,
            fileSystem,
            environmentVariables,
            workingDirectory,
            sourceSchemaFiles,
            remoteSourceSchemaInputs,
            archiveFile,
            environment,
            compositionSettings,
            httpClient,
            watchedSourceSchemaNames: null,
            removeSourceSchemas,
            cancellationToken);
    }

    private static async Task<int> WatchComposeAsync(
        INitroConsole console,
        IFileSystem fileSystem,
        IEnvironmentVariableProvider environmentVariables,
        string workingDirectory,
        List<string> sourceSchemaFiles,
        List<RemoteSourceSchemaInput> remoteSourceSchemaInputs,
        string archiveFile,
        string? environment,
        CompositionSettings compositionSettings,
        HttpClient? httpClient,
        CancellationToken cancellationToken)
    {
        console.WriteLine("🔍 Starting watch mode...");
        var watchedSourceSchemaNames = new HashSet<string>(StringComparer.Ordinal);

        // Initial composition
        var initialResult = await ComposeAsync(
            console,
            fileSystem,
            environmentVariables,
            workingDirectory,
            sourceSchemaFiles,
            remoteSourceSchemaInputs,
            archiveFile,
            environment,
            compositionSettings,
            httpClient,
            watchedSourceSchemaNames,
            [],
            cancellationToken);

        if (initialResult != 0)
        {
            return initialResult;
        }

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
            workingDirectory,
            sourceSchemaFiles,
            remoteSourceSchemaInputs,
            archiveFile,
            environment,
            compositionSettings,
            httpClient,
            watchedSourceSchemaNames,
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
            else if (FusionCompositionHelpers.IsSchemaFile(sourceSchemaPath))
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

        foreach (var remoteSourceSchemaInput in remoteSourceSchemaInputs)
        {
            CreateRemoteSourceSchemaSettingsWatcher(
                sourceSchemaFileWatchers,
                remoteSourceSchemaInput.SettingsFile,
                compositionChannel.Writer);
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
            var directoryName = Path.GetDirectoryName(e.FullPath)!;

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

    private static void CreateRemoteSourceSchemaSettingsWatcher(
        List<FileSystemWatcher> watchers,
        string sourceSchemaSettingsFile,
        ChannelWriter<string> writer)
    {
        var watcher = new FileSystemWatcher(
            Path.GetDirectoryName(sourceSchemaSettingsFile)!,
            Path.GetFileName(sourceSchemaSettingsFile))
        {
            NotifyFilter = NotifyFilters.CreationTime
                | NotifyFilters.LastWrite
                | NotifyFilters.FileName,
            EnableRaisingEvents = true
        };

        watcher.Changed += (_, e) => TriggerComposition(e);
        watcher.Created += (_, e) => TriggerComposition(e);
        watcher.Deleted += (_, e) => TriggerComposition(e);
        watcher.Renamed += (_, e) => TriggerComposition(e);
        watchers.Add(watcher);

        void TriggerComposition(FileSystemEventArgs eventArgs)
            => writer.TryWrite(
                "Source schema settings file "
                + $"{eventArgs.ChangeType.ToString().ToLowerInvariant()}: {eventArgs.FullPath}");
    }

    private static async Task ProcessCompositionRequestsAsync(
        ChannelReader<string> reader,
        INitroConsole console,
        IFileSystem fileSystem,
        IEnvironmentVariableProvider environmentVariables,
        string workingDirectory,
        List<string> sourceSchemaFiles,
        List<RemoteSourceSchemaInput> remoteSourceSchemaInputs,
        string archiveFile,
        string? environment,
        CompositionSettings compositionSettings,
        HttpClient? httpClient,
        HashSet<string> watchedSourceSchemaNames,
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
                    workingDirectory,
                    sourceSchemaFiles,
                    remoteSourceSchemaInputs,
                    archiveFile,
                    environment,
                    compositionSettings,
                    httpClient,
                    watchedSourceSchemaNames,
                    [],
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

    private static async Task<int> ComposeAsync(
        INitroConsole console,
        IFileSystem fileSystem,
        IEnvironmentVariableProvider environmentVariables,
        string workingDirectory,
        List<string> sourceSchemaFiles,
        List<RemoteSourceSchemaInput> remoteSourceSchemaInputs,
        string archiveFile,
        string? environment,
        CompositionSettings compositionSettings,
        HttpClient? httpClient,
        HashSet<string>? watchedSourceSchemaNames,
        IReadOnlyList<string> removeSourceSchemas,
        CancellationToken cancellationToken)
    {
        environment ??= environmentVariables.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        var ownedSettings = new List<JsonDocument>();

        try
        {
            var sourceSchemas = await FusionCompositionHelpers.ReadSourceSchemasAsync(
                fileSystem,
                workingDirectory,
                sourceSchemaFiles,
                cancellationToken);
            ownedSettings.AddRange(sourceSchemas.Values.Select(value => value.Item2));

            if (watchedSourceSchemaNames is { Count: > 0 })
            {
                var remoteSourceSchemaNames = remoteSourceSchemaInputs
                    .Select(input => input.Name)
                    .Where(static name => name is not null)
                    .ToHashSet(StringComparer.Ordinal);
                var expectedLocalSourceSchemaNames = watchedSourceSchemaNames
                    .Where(name => !remoteSourceSchemaNames.Contains(name))
                    .ToHashSet(StringComparer.Ordinal);

                if (!expectedLocalSourceSchemaNames.SetEquals(sourceSchemas.Keys))
                {
                    throw new ExitException(Messages.WatchedSourceSchemaNameChanged());
                }
            }

            if (remoteSourceSchemaInputs.Count > 0)
            {
                var remoteSourceSchemas = await FusionCompositionHelpers
                    .FetchRemoteSourceSchemasAsync(
                        fileSystem,
                        remoteSourceSchemaInputs,
                        sourceSchemas.Keys.ToHashSet(StringComparer.Ordinal),
                        httpClient!,
                        cancellationToken);
                ownedSettings.AddRange(
                    remoteSourceSchemas.Values.Select(value => value.Item2));

                foreach (var (sourceSchemaName, sourceSchema) in remoteSourceSchemas)
                {
                    if (!sourceSchemas.TryAdd(sourceSchemaName, sourceSchema))
                    {
                        throw new ExitException(
                            Messages.DuplicateSourceSchemaName(sourceSchemaName));
                    }
                }
            }

            if (watchedSourceSchemaNames is not null)
            {
                if (watchedSourceSchemaNames.Count == 0)
                {
                    watchedSourceSchemaNames.UnionWith(sourceSchemas.Keys);
                }
                else if (!watchedSourceSchemaNames.SetEquals(sourceSchemas.Keys))
                {
                    throw new ExitException(Messages.WatchedSourceSchemaNameChanged());
                }
            }

            using var archive = fileSystem.FileExists(archiveFile) || File.Exists(archiveFile)
                ? FusionArchive.Open(archiveFile, mode: FusionArchiveMode.Update)
                : FusionArchive.Create(archiveFile);

            if (removeSourceSchemas.Count > 0)
            {
                var existing = (await archive.GetSourceSchemaNamesAsync(cancellationToken))
                    .ToHashSet(StringComparer.Ordinal);

                var missingSourceSchema =
                    removeSourceSchemas.FirstOrDefault(name => !existing.Contains(name));

                if (missingSourceSchema is not null)
                {
                    console.Error.WriteErrorLine(
                        Messages.SourceSchemaDoesNotExistInArchive(missingSourceSchema, archiveFile));
                    return 1;
                }

                foreach (var name in removeSourceSchemas)
                {
                    await archive.RemoveSourceSchemaConfigurationAsync(name, cancellationToken);
                }
            }

            var compositionLog = new CompositionLog();

            var result = await CompositionHelper.ComposeAsync(
                compositionLog,
                sourceSchemas,
                archive,
                environment,
                compositionSettings,
                legacyArchive: null,
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

            console.WriteLine($"✅ Composite schema written to '{archiveFile}'.");

            return 0;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception e)
        {
            console.Error.WriteErrorLine(e.Message.EscapeMarkup());
            return 1;
        }
        finally
        {
            foreach (var settings in ownedSettings)
            {
                settings.Dispose();
            }
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
