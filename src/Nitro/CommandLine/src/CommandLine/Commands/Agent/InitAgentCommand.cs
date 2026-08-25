using System.Text;
using System.Text.Json;
using ChilliCream.Nitro.CommandLine.Commands.Agent.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Memory;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using Dapper;
using Microsoft.Data.Sqlite;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent;

/// <summary>
/// Initializes the unified agent workspace: one <c>agents.db</c>, the source
/// of truth shared by the task tracker and mail, at <c>.git/nitro</c> in a
/// git repository or <c>.nitro/agents</c> otherwise (an existing
/// <c>.nitro/agents</c> takes precedence). Also migrates a legacy
/// <c>.nitro/tasks</c> workspace, imports and removes a <c>tasks.jsonl</c>
/// left over from the retired JSONL sync model, and with <c>--migrate</c>
/// moves a <c>.nitro/agents</c> workspace into <c>.git/nitro</c>.
/// </summary>
internal sealed class InitAgentCommand : Command
{
    private static readonly string[] LegacyTaskTables =
        ["tasks", "dependencies", "labels", "comments", "events", "config", "child_counters"];

    public InitAgentCommand() : base("init")
    {
        Description = "Initialize an agent workspace in the current directory.";

        Options.Add(Opt<AgentPrefixOption>.Instance);
        Options.Add(Opt<ForceReinitializeAgentOption>.Instance);
        Options.Add(Opt<MigrateAgentOption>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        Validators.Add(result =>
        {
            if (result.GetValue(Opt<MigrateAgentOption>.Instance)
                && (result.GetValue(Opt<ForceReinitializeAgentOption>.Instance)
                    || result.GetValue(Opt<AgentPrefixOption>.Instance) is not null))
            {
                result.AddError("'--migrate' cannot be combined with '--force' or '--prefix'.");
            }
        });

        this.AddExamples("agent init", "agent init --prefix \"app\"", "agent init --migrate");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var fileSystem = services.GetRequiredService<IFileSystem>();
        var store = services.GetRequiredService<ITaskStore>();
        var memoryStore = services.GetRequiredService<IMemoryStore>();
        var resultHolder = services.GetRequiredService<IResultHolder>();
        var database = services.GetRequiredService<AgentDatabase>();

        var currentDirectory = fileSystem.GetCurrentDirectory();

        if (parseResult.GetValue(Opt<MigrateAgentOption>.Instance))
        {
            return await MigrateAsync(
                console, fileSystem, store, resultHolder, database, currentDirectory, cancellationToken);
        }

        // Location resolution: an initialized workspace anywhere above wins
        // (a .nitro/agents database before the repository's .git/nitro at
        // each level); else an existing bare .nitro/agents directory (a
        // fresh clone may carry committed memory markdown with no database
        // yet); else the nearest repository's .git/nitro; else a fresh
        // .nitro/agents under the current directory.
        var location = AgentWorkspace.FindLocation(fileSystem, currentDirectory)
            ?? AgentWorkspace.FindFallbackDirectory(fileSystem, currentDirectory)
            ?? AgentWorkspace.FindGitWorkspace(fileSystem, currentDirectory)
            ?? new WorkspaceLocation(currentDirectory, AgentWorkspace.GetDirectory(currentDirectory));

        var workspaceDirectory = location.WorkspaceDirectory;
        var projectDirectory = location.ProjectDirectory;
        var displayPath = AgentWorkspace.GetDisplayPath(workspaceDirectory);
        var isFallbackLayout = AgentWorkspace.IsFallbackLayout(workspaceDirectory);

        var gitWorkspace = AgentWorkspace.FindGitWorkspace(fileSystem, currentDirectory);
        var migrateAvailable = isFallbackLayout && gitWorkspace is not null;

        var databasePath = AgentWorkspace.GetDatabasePath(workspaceDirectory);
        var jsonlPath = AgentWorkspace.GetLegacyJsonlPath(workspaceDirectory);
        var gitIgnorePath = Path.Combine(workspaceDirectory, AgentWorkspace.GitIgnoreFileName);

        var force = parseResult.GetValue(Opt<ForceReinitializeAgentOption>.Instance);
        var explicitPrefix = parseResult.GetValue(Opt<AgentPrefixOption>.Instance);
        var directoryDefaultPrefix = AgentWorkspace.NormalizePrefix(
            Path.GetFileName(Path.TrimEndingDirectorySeparator(projectDirectory)));

        var databaseAlreadyExists = fileSystem.FileExists(databasePath);

        if (databaseAlreadyExists && !force)
        {
            var existingVersion = await database.ReadVersionAsync(workspaceDirectory, cancellationToken);

            if (!AgentDatabase.IsUpgradableVersion(existingVersion))
            {
                throw new ExitException(
                    migrateAvailable
                        ? $"Already initialized at '{displayPath}'. Use --force to reinitialize, or "
                            + "`nitro agent init --migrate` to move the workspace into "
                            + $"'{AgentWorkspace.GitDisplayPath}'."
                        : $"Already initialized at '{displayPath}'. Use --force to reinitialize.");
            }

            // An existing database at an upgradable schema version: plain
            // init applies the non-destructive schema upgrade only, no
            // prefix or gitignore refresh, instead of throwing. This is what
            // makes the already-shipped connect error text ("Run
            // `nitro agent init` to migrate it") literally true. A database
            // newer than this CLI understands still throws here, inside
            // InitializeAsync, regardless of --force.
            await using (await database.InitializeAsync(workspaceDirectory, cancellationToken))
            {
            }

            var upgradeImportedCount = await ImportAndRemoveLegacyJsonlAsync(
                store, fileSystem, jsonlPath, cancellationToken);

            var upgradedPrefix = await store.GetPrefixAsync(cancellationToken);

            return WriteUpgradeResult(
                console,
                resultHolder,
                workspaceDirectory,
                upgradedPrefix,
                upgradeImportedCount,
                isFallbackLayout,
                migrateAvailable);
        }

        if (databaseAlreadyExists)
        {
            // --force against an existing unified workspace: reapply the
            // schema in place (non-destructive), refresh the prefix and
            // gitignore, and clean up any leftover tasks.jsonl.
            var reinitPrefix = AgentWorkspace.NormalizePrefix(explicitPrefix ?? directoryDefaultPrefix);

            await store.InitializeWorkspaceAsync(workspaceDirectory, reinitPrefix, cancellationToken);
            await memoryStore.EnsureProjectWorkspaceAsync(workspaceDirectory, cancellationToken);

            if (isFallbackLayout)
            {
                await WriteGitIgnoreAsync(fileSystem, gitIgnorePath, force: true, cancellationToken);
            }

            var reinitImportedCount = await ImportAndRemoveLegacyJsonlAsync(
                store, fileSystem, jsonlPath, cancellationToken);

            return WriteResult(
                console,
                resultHolder,
                workspaceDirectory,
                reinitPrefix,
                migratedTasks: 0,
                importedCount: reinitImportedCount,
                removedJsonl: reinitImportedCount is not null,
                isFallbackLayout,
                migrateAvailable);
        }

        var legacyTasksDirectory = Path.Combine(projectDirectory, ".nitro", "tasks");
        var legacyTasksDatabasePath = Path.Combine(legacyTasksDirectory, "tasks.db");
        var legacyTasksJsonlPath = Path.Combine(legacyTasksDirectory, "tasks.jsonl");
        var legacyMailDatabasePath = Path.Combine(projectDirectory, ".nitro", "mail", "mail.db");

        var hasLegacyTasksDatabase = fileSystem.FileExists(legacyTasksDatabasePath);
        var hasLegacyTasksJsonl = fileSystem.FileExists(legacyTasksJsonlPath);
        var hasLegacyMailDatabase = fileSystem.FileExists(legacyMailDatabasePath);
        var hasWorkspaceJsonl = fileSystem.FileExists(jsonlPath);

        // Every found tasks.jsonl is imported: one at the OLD legacy path
        // predates the unified layout, one at the workspace path is a
        // leftover of the retired JSONL sync model (for example a fresh
        // clone that committed it).
        var jsonlSourcePaths = new List<string>();

        if (hasLegacyTasksJsonl)
        {
            jsonlSourcePaths.Add(legacyTasksJsonlPath);
        }

        if (hasWorkspaceJsonl)
        {
            jsonlSourcePaths.Add(jsonlPath);
        }

        // Parse every source jsonl BEFORE creating anything, so a malformed
        // file leaves no initialized workspace and the command is retryable.
        var jsonlConfig = new List<TaskConfigEntry>();
        var jsonlRecords = new List<TaskSyncRecord>();

        foreach (var sourcePath in jsonlSourcePaths)
        {
            var (config, records) =
                await ReadLegacyJsonlAsync(fileSystem, sourcePath, cancellationToken);
            jsonlConfig.AddRange(config);
            jsonlRecords.AddRange(records);
        }

        var createdDatabase = false;
        var migratedTasks = 0;
        int? importedCount = null;
        string prefix;

        try
        {
            if (!fileSystem.DirectoryExists(workspaceDirectory))
            {
                fileSystem.CreateDirectory(workspaceDirectory);
            }

            await store.EnsureWorkspaceAsync(workspaceDirectory, cancellationToken);
            createdDatabase = true;

            await memoryStore.EnsureProjectWorkspaceAsync(workspaceDirectory, cancellationToken);

            if (hasLegacyTasksDatabase)
            {
                migratedTasks = await CopyLegacyTasksDatabaseAsync(
                    databasePath, legacyTasksDatabasePath, cancellationToken);
            }

            if (jsonlSourcePaths.Count > 0)
            {
                var importResult = await store.ImportTasksAsync(jsonlRecords, cancellationToken);
                importedCount = importResult.Applied;

                foreach (var entry in jsonlConfig)
                {
                    await store.SetConfigAsync(entry.Key, entry.Value, cancellationToken);
                }
            }

            if (explicitPrefix is not null)
            {
                prefix = AgentWorkspace.NormalizePrefix(explicitPrefix);
                await store.SetConfigAsync("prefix", prefix, cancellationToken);
            }
            else
            {
                var migratedPrefix = await store.GetConfigAsync("prefix", cancellationToken);
                prefix = migratedPrefix ?? directoryDefaultPrefix;

                if (migratedPrefix is null)
                {
                    await store.SetConfigAsync("prefix", prefix, cancellationToken);
                }
            }
        }
        catch
        {
            // No partial workspace survives a failed init; the files this
            // reads from are never touched before this point, so the command
            // is retryable afterward.
            if (createdDatabase && fileSystem.FileExists(databasePath))
            {
                fileSystem.DeleteFile(databasePath);
            }

            throw;
        }

        // The database is durable from here on: the workspace tasks.jsonl is
        // deleted only once its content is safely imported, and a later
        // failure no longer rolls the database back. A legacy-path
        // tasks.jsonl is left for the printed `git rm` cleanup of the whole
        // legacy directory.
        var removedJsonl = false;

        if (hasWorkspaceJsonl)
        {
            fileSystem.DeleteFile(jsonlPath);
            removedJsonl = true;
        }

        if (isFallbackLayout)
        {
            await WriteGitIgnoreAsync(fileSystem, gitIgnorePath, force: false, cancellationToken);
        }

        if (console.IsHumanReadable)
        {
            console.OkLine($"Initialized agent workspace at '{displayPath}'.");
            console.OkLine($"Task ID prefix set to '{prefix}'.");

            if (migratedTasks > 0)
            {
                console.OkLine(
                    $"Migrated {migratedTasks} {(migratedTasks == 1 ? "task" : "tasks")} "
                    + "from the legacy '.nitro/tasks' workspace.");
            }

            if (importedCount is not null)
            {
                console.OkLine(
                    $"Imported {importedCount} {(importedCount == 1 ? "task" : "tasks")} "
                    + $"from '{AgentWorkspace.LegacyJsonlFileName}'.");
            }

            if (removedJsonl)
            {
                WriteJsonlRemovedLines(console, isFallbackLayout);
            }

            if (hasLegacyTasksDatabase || hasLegacyTasksJsonl)
            {
                console.WriteLine();
                console.WriteLine(
                    "The legacy '.nitro/tasks' workspace has been migrated. Remove it from git with:");
                console.WriteLine("  git rm -r .nitro/tasks");

                if (isFallbackLayout)
                {
                    console.WriteLine($"  git add {displayPath}");
                }
            }

            if (hasLegacyMailDatabase)
            {
                console.WriteLine();
                console.WriteLine(
                    "Found a legacy mail workspace at '.nitro/mail'. It was never released, "
                    + "so its data was not migrated. Delete it with:");
                console.WriteLine("  rm -rf .nitro/mail");
            }

            if (migrateAvailable)
            {
                WriteMigrateHint(console);
            }
        }

        return WriteJsonResult(console, resultHolder, workspaceDirectory, prefix, migratedTasks, importedCount);
    }

    /// <summary>
    /// Moves an existing <c>.nitro/agents</c> workspace into the
    /// repository's <c>.git/nitro</c> directory, then applies the schema
    /// upgrade and the leftover tasks.jsonl cleanup at the new location.
    /// </summary>
    private static async Task<int> MigrateAsync(
        INitroConsole console,
        IFileSystem fileSystem,
        ITaskStore store,
        IResultHolder resultHolder,
        AgentDatabase database,
        string currentDirectory,
        CancellationToken cancellationToken)
    {
        var existing = AgentWorkspace.FindLocation(fileSystem, currentDirectory)
            ?? throw new ExitException("No agent workspace found. Run `nitro agent init` first.");

        var gitWorkspace = AgentWorkspace.FindGitWorkspace(fileSystem, currentDirectory)
            ?? throw new ExitException(
                "No git repository found. '--migrate' moves the workspace into the "
                + "repository's .git directory.");

        var sourceDirectory = existing.WorkspaceDirectory;
        var targetDirectory = gitWorkspace.WorkspaceDirectory;
        var sourceDisplay = AgentWorkspace.GetDisplayPath(sourceDirectory);
        var targetDisplay = AgentWorkspace.GetDisplayPath(targetDirectory);

        if (string.Equals(
            Path.GetFullPath(sourceDirectory),
            Path.GetFullPath(targetDirectory),
            StringComparison.Ordinal))
        {
            console.OkLine($"Workspace already at '{targetDisplay}'; nothing to migrate.");

            return WriteMigrateResult(
                console, resultHolder, sourceDirectory, targetDirectory, importedCount: null);
        }

        if (fileSystem.DirectoryExists(targetDirectory))
        {
            throw new ExitException(
                $"'{targetDisplay}' already exists. Remove it before migrating '{sourceDisplay}'.");
        }

        fileSystem.MoveDirectory(sourceDirectory, targetDirectory);

        // The fallback layout's .gitignore is meaningless inside .git.
        var gitIgnorePath = Path.Combine(targetDirectory, AgentWorkspace.GitIgnoreFileName);

        if (fileSystem.FileExists(gitIgnorePath))
        {
            fileSystem.DeleteFile(gitIgnorePath);
        }

        TryDeleteEmptyNitroRoot(fileSystem, sourceDirectory);

        await using (await database.InitializeAsync(targetDirectory, cancellationToken))
        {
        }

        var importedCount = await ImportAndRemoveLegacyJsonlAsync(
            store, fileSystem, AgentWorkspace.GetLegacyJsonlPath(targetDirectory), cancellationToken);

        if (console.IsHumanReadable)
        {
            console.OkLine($"Moved agent workspace from '{sourceDisplay}' to '{targetDisplay}'.");

            if (importedCount is not null)
            {
                console.OkLine(
                    $"Imported {importedCount} {(importedCount == 1 ? "task" : "tasks")} "
                    + $"from '{AgentWorkspace.LegacyJsonlFileName}' and removed it.");
            }

            console.WriteLine();
            console.WriteLine($"If '{sourceDisplay}' was committed, remove it from git with:");
            console.WriteLine($"  git rm -r --cached {sourceDisplay}");
        }

        return WriteMigrateResult(console, resultHolder, sourceDirectory, targetDirectory, importedCount);
    }

    /// <summary>
    /// Deletes the <c>.nitro</c> directory a migrated workspace leaves
    /// behind, but only when it is empty; a leftover legacy
    /// <c>.nitro/tasks</c> keeps it in place.
    /// </summary>
    private static void TryDeleteEmptyNitroRoot(IFileSystem fileSystem, string sourceWorkspaceDirectory)
    {
        var nitroRoot = Path.GetDirectoryName(sourceWorkspaceDirectory);

        if (nitroRoot is null
            || Path.GetFileName(nitroRoot) != AgentWorkspace.RootDirectoryName
            || !fileSystem.DirectoryExists(nitroRoot))
        {
            return;
        }

        try
        {
            fileSystem.DeleteDirectory(nitroRoot, recursive: false);
        }
        catch (IOException)
        {
        }
    }

    private static int WriteMigrateResult(
        INitroConsole console,
        IResultHolder resultHolder,
        string fromDirectory,
        string toDirectory,
        int? importedCount)
    {
        if (console.IsHumanReadable)
        {
            return ExitCodes.Success;
        }

        resultHolder.SetResult(
            new ObjectResult(new AgentWorkspaceMigrateResult(fromDirectory, toDirectory, importedCount)));

        return ExitCodes.Success;
    }

    /// <summary>
    /// Prints the base "Initialized..." lines for a human-readable console,
    /// or sets the JSON result otherwise. Callers that print additional
    /// human-readable detail lines call this first, so those lines follow
    /// rather than precede the base ones.
    /// </summary>
    private static int WriteResult(
        INitroConsole console,
        IResultHolder resultHolder,
        string workspaceDirectory,
        string prefix,
        int migratedTasks,
        int? importedCount,
        bool removedJsonl,
        bool isFallbackLayout,
        bool migrateAvailable)
    {
        if (!console.IsHumanReadable)
        {
            return WriteJsonResult(console, resultHolder, workspaceDirectory, prefix, migratedTasks, importedCount);
        }

        console.OkLine(
            $"Initialized agent workspace at '{AgentWorkspace.GetDisplayPath(workspaceDirectory)}'.");
        console.OkLine($"Task ID prefix set to '{prefix}'.");

        if (importedCount is not null)
        {
            console.OkLine(
                $"Imported {importedCount} {(importedCount == 1 ? "task" : "tasks")} "
                + $"from '{AgentWorkspace.LegacyJsonlFileName}'.");
        }

        if (removedJsonl)
        {
            WriteJsonlRemovedLines(console, isFallbackLayout);
        }

        if (migrateAvailable)
        {
            WriteMigrateHint(console);
        }

        return ExitCodes.Success;
    }

    /// <summary>
    /// Prints the "Upgraded..." line for a human-readable console, or sets
    /// the JSON result otherwise, for a plain `init` against an existing
    /// database at an upgradable schema version. Distinct from
    /// <see cref="WriteResult"/>: nothing was freshly initialized, and the
    /// prefix was read back unchanged, not set.
    /// </summary>
    private static int WriteUpgradeResult(
        INitroConsole console,
        IResultHolder resultHolder,
        string workspaceDirectory,
        string prefix,
        int? importedCount,
        bool isFallbackLayout,
        bool migrateAvailable)
    {
        if (!console.IsHumanReadable)
        {
            return WriteJsonResult(
                console, resultHolder, workspaceDirectory, prefix, migratedTasks: 0, importedCount);
        }

        console.OkLine(
            "Upgraded agent workspace schema at "
            + $"'{AgentWorkspace.GetDisplayPath(workspaceDirectory)}' to v{AgentDatabase.CurrentVersion}.");

        if (importedCount is not null)
        {
            console.OkLine(
                $"Imported {importedCount} {(importedCount == 1 ? "task" : "tasks")} "
                + $"from '{AgentWorkspace.LegacyJsonlFileName}'.");
            WriteJsonlRemovedLines(console, isFallbackLayout);
        }

        if (migrateAvailable)
        {
            WriteMigrateHint(console);
        }

        return ExitCodes.Success;
    }

    private static void WriteJsonlRemovedLines(INitroConsole console, bool isFallbackLayout)
    {
        console.OkLine(
            $"Removed '{AgentWorkspace.LegacyJsonlFileName}'; the task database is the source of truth.");

        if (!isFallbackLayout)
        {
            return;
        }

        console.WriteLine();
        console.WriteLine(
            $"If '{AgentWorkspace.LegacyJsonlFileName}' was committed, remove it from git with:");
        console.WriteLine(
            $"  git rm --cached {AgentWorkspace.DisplayPath}/{AgentWorkspace.LegacyJsonlFileName}");
    }

    private static void WriteMigrateHint(INitroConsole console)
    {
        console.WriteLine();
        console.WriteLine(
            "Run `nitro agent init --migrate` to move this workspace into "
            + $"'{AgentWorkspace.GitDisplayPath}'.");
    }

    private static int WriteJsonResult(
        INitroConsole console,
        IResultHolder resultHolder,
        string workspaceDirectory,
        string prefix,
        int migratedTasks,
        int? importedCount)
    {
        if (console.IsHumanReadable)
        {
            return ExitCodes.Success;
        }

        resultHolder.SetResult(
            new ObjectResult(
                new AgentWorkspaceInitResult(workspaceDirectory, prefix, migratedTasks, importedCount)));

        return ExitCodes.Success;
    }

    private static async Task WriteGitIgnoreAsync(
        IFileSystem fileSystem,
        string gitIgnorePath,
        bool force,
        CancellationToken cancellationToken)
    {
        if (force || !fileSystem.FileExists(gitIgnorePath))
        {
            await using var gitIgnoreStream = fileSystem.CreateFile(gitIgnorePath);
            await gitIgnoreStream.WriteAsync(
                Encoding.UTF8.GetBytes(AgentWorkspace.GitIgnoreContent), cancellationToken);
        }
    }

    /// <summary>
    /// Imports a leftover workspace tasks.jsonl into the task database,
    /// filling only config keys the database does not have yet, then deletes
    /// the file. Returns the applied task count, or null when no file exists.
    /// </summary>
    private static async Task<int?> ImportAndRemoveLegacyJsonlAsync(
        ITaskStore store,
        IFileSystem fileSystem,
        string jsonlPath,
        CancellationToken cancellationToken)
    {
        if (!fileSystem.FileExists(jsonlPath))
        {
            return null;
        }

        var (config, records) = await ReadLegacyJsonlAsync(fileSystem, jsonlPath, cancellationToken);

        var result = await store.ImportTasksAsync(records, cancellationToken);

        foreach (var entry in config)
        {
            if (await store.GetConfigAsync(entry.Key, cancellationToken) is null)
            {
                await store.SetConfigAsync(entry.Key, entry.Value, cancellationToken);
            }
        }

        fileSystem.DeleteFile(jsonlPath);

        return result.Applied;
    }

    /// <summary>
    /// Parses a legacy tasks.jsonl, splitting its lines into workspace config
    /// entries and task records. A line is a config entry when it has no "id"
    /// property, which every task line carries; a config line instead carries
    /// "key" and "value".
    /// </summary>
    private static async Task<(IReadOnlyList<TaskConfigEntry> Config, IReadOnlyList<TaskSyncRecord> Records)>
        ReadLegacyJsonlAsync(
            IFileSystem fileSystem,
            string jsonlPath,
            CancellationToken cancellationToken)
    {
        var content = await fileSystem.ReadAllTextAsync(jsonlPath, cancellationToken);
        var config = new List<TaskConfigEntry>();
        var records = new List<TaskSyncRecord>();
        var lineNumber = 0;

        foreach (var line in content.ReplaceLineEndings("\n").Split('\n'))
        {
            lineNumber++;

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            try
            {
                using var document = JsonDocument.Parse(line);

                if (document.RootElement.ValueKind is not JsonValueKind.Object)
                {
                    throw new ExitException(
                        $"'{AgentWorkspace.LegacyJsonlFileName}' line {lineNumber} is not a JSON object.");
                }

                if (document.RootElement.TryGetProperty("id", out _))
                {
                    records.Add(
                        JsonSerializer.Deserialize(line, TaskSyncJsonContext.Default.TaskSyncRecord)
                        ?? throw new ExitException(
                            $"'{AgentWorkspace.LegacyJsonlFileName}' line {lineNumber} is empty."));
                }
                else
                {
                    config.Add(
                        JsonSerializer.Deserialize(line, TaskSyncJsonContext.Default.TaskConfigEntry)
                        ?? throw new ExitException(
                            $"'{AgentWorkspace.LegacyJsonlFileName}' line {lineNumber} is empty."));
                }
            }
            catch (JsonException exception)
            {
                throw new ExitException(
                    $"'{AgentWorkspace.LegacyJsonlFileName}' line {lineNumber} is not valid JSON: "
                    + exception.Message);
            }
        }

        return (config, records);
    }

    /// <summary>
    /// Attaches a legacy <c>tasks.db</c> (schema v1, the pre-unification
    /// task-only database) to the freshly created unified database and
    /// copies every one of its tables into the corresponding unified table
    /// in one transaction; the tables are identical between the two schema
    /// versions. Returns the resulting task count.
    /// </summary>
    private static async Task<int> CopyLegacyTasksDatabaseAsync(
        string databasePath,
        string legacyDatabasePath,
        CancellationToken cancellationToken)
    {
        await using var connection = new SqliteConnection($"Data Source={databasePath};Pooling=False");
        await connection.OpenAsync(cancellationToken);

        var escapedLegacyPath = legacyDatabasePath.Replace("'", "''");
        await connection.ExecuteAsync($"ATTACH DATABASE '{escapedLegacyPath}' AS legacy;");

        try
        {
            await using (var transaction = await connection.BeginTransactionAsync(cancellationToken))
            {
                foreach (var table in LegacyTaskTables)
                {
                    await connection.ExecuteAsync(
                        $"INSERT INTO main.{table} SELECT * FROM legacy.{table};", transaction: transaction);
                }

                await transaction.CommitAsync(cancellationToken);
            }
        }
        finally
        {
            await connection.ExecuteAsync("DETACH DATABASE legacy;");
        }

        return (int)await connection.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM main.tasks;");
    }

    public sealed record AgentWorkspaceInitResult(
        string Path, string Prefix, int MigratedTasks, int? ImportedCount);

    public sealed record AgentWorkspaceMigrateResult(string From, string To, int? ImportedCount);
}
