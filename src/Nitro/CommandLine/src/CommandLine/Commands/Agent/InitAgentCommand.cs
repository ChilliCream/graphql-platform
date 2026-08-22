using System.Text;
using ChilliCream.Nitro.CommandLine.Commands.Agent.Options;
using ChilliCream.Nitro.CommandLine.Commands.Tasks;
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
/// Initializes the unified agent workspace: the single <c>.nitro/agents</c>
/// directory, one <c>agents.db</c>, and <c>tasks.jsonl</c>, shared by the
/// task tracker and mail. Replaces the retired <c>agent tasks init</c> and
/// <c>agent mail init</c> commands, and migrates a legacy <c>.nitro/tasks</c>
/// and/or <c>.nitro/mail</c> workspace from before the two were unified.
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
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples("agent init", "agent init --prefix \"app\"");

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
        var workspaceDirectory = AgentWorkspace.GetDirectory(currentDirectory);
        var databasePath = AgentWorkspace.GetDatabasePath(workspaceDirectory);
        var jsonlPath = AgentWorkspace.GetJsonlPath(workspaceDirectory);
        var gitIgnorePath = Path.Combine(workspaceDirectory, AgentWorkspace.GitIgnoreFileName);

        var force = parseResult.GetValue(Opt<ForceReinitializeAgentOption>.Instance);
        var explicitPrefix = parseResult.GetValue(Opt<AgentPrefixOption>.Instance);
        var directoryDefaultPrefix = AgentWorkspace.NormalizePrefix(
            Path.GetFileName(Path.TrimEndingDirectorySeparator(currentDirectory)));

        var databaseAlreadyExists = fileSystem.FileExists(databasePath);

        if (databaseAlreadyExists && !force)
        {
            var existingVersion = await database.ReadVersionAsync(workspaceDirectory, cancellationToken);

            if (!AgentDatabase.IsUpgradableVersion(existingVersion))
            {
                throw new ExitException(
                    $"Already initialized at '{AgentWorkspace.DisplayPath}'. "
                    + "Use --force to reinitialize.");
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

            var upgradedPrefix = await store.GetPrefixAsync(cancellationToken);

            return WriteUpgradeResult(console, resultHolder, workspaceDirectory, upgradedPrefix);
        }

        if (databaseAlreadyExists)
        {
            // --force against an existing unified workspace: reapply the
            // schema in place (non-destructive), refresh the prefix and
            // gitignore. Never touches tasks.jsonl.
            var reinitPrefix = AgentWorkspace.NormalizePrefix(explicitPrefix ?? directoryDefaultPrefix);

            await store.InitializeWorkspaceAsync(workspaceDirectory, reinitPrefix, cancellationToken);
            await memoryStore.EnsureProjectWorkspaceAsync(workspaceDirectory, cancellationToken);
            await WriteGitIgnoreAsync(fileSystem, gitIgnorePath, force: true, cancellationToken);

            return WriteResult(
                console, resultHolder, workspaceDirectory, reinitPrefix, migratedTasks: 0, importedCount: null);
        }

        var legacyTasksDirectory = Path.Combine(currentDirectory, ".nitro", "tasks");
        var legacyTasksDatabasePath = Path.Combine(legacyTasksDirectory, "tasks.db");
        var legacyTasksJsonlPath = Path.Combine(legacyTasksDirectory, "tasks.jsonl");
        var legacyMailDatabasePath = Path.Combine(currentDirectory, ".nitro", "mail", "mail.db");

        var hasLegacyTasksDatabase = fileSystem.FileExists(legacyTasksDatabasePath);
        var hasLegacyTasksJsonl = fileSystem.FileExists(legacyTasksJsonlPath);
        var hasLegacyMailDatabase = fileSystem.FileExists(legacyMailDatabasePath);
        var hasCommittedJsonlOnly = fileSystem.FileExists(jsonlPath);

        // A committed tasks.jsonl at the NEW path with no db is a fresh
        // clone of the already-unified layout; a tasks.jsonl at the OLD
        // legacy path is what gets reconciled below instead.
        var jsonlSourcePath = hasLegacyTasksJsonl
            ? legacyTasksJsonlPath
            : hasCommittedJsonlOnly
                ? jsonlPath
                : null;

        // Parse any source jsonl BEFORE creating anything, so a malformed
        // file leaves no initialized workspace and the command is retryable.
        IReadOnlyList<TaskConfigEntry> jsonlConfig = [];
        IReadOnlyList<TaskSyncRecord> jsonlRecords = [];

        if (jsonlSourcePath is not null)
        {
            (jsonlConfig, jsonlRecords) =
                await SyncTaskCommand.ReadJsonlAsync(fileSystem, jsonlSourcePath, cancellationToken);
        }

        var createdDatabase = false;

        try
        {
            if (!fileSystem.DirectoryExists(workspaceDirectory))
            {
                fileSystem.CreateDirectory(workspaceDirectory);
            }

            await store.EnsureWorkspaceAsync(workspaceDirectory, cancellationToken);
            createdDatabase = true;

            await memoryStore.EnsureProjectWorkspaceAsync(workspaceDirectory, cancellationToken);

            var migratedTasks = 0;

            if (hasLegacyTasksDatabase)
            {
                migratedTasks = await CopyLegacyTasksDatabaseAsync(
                    databasePath, legacyTasksDatabasePath, cancellationToken);
            }

            int? importedCount = null;

            if (jsonlSourcePath is not null)
            {
                var importResult = await store.ImportTasksAsync(jsonlRecords, cancellationToken);
                importedCount = importResult.Applied;

                foreach (var entry in jsonlConfig)
                {
                    await store.SetConfigAsync(entry.Key, entry.Value, cancellationToken);
                }
            }

            string prefix;

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

            // The committed layout (row 4: fresh clone, no db) already owns
            // its tasks.jsonl and .gitignore; only the database is new here,
            // so those committed files are left exactly as they are.
            if (jsonlSourcePath != jsonlPath)
            {
                var config = await store.ListConfigAsync(cancellationToken);
                var records = await store.ExportTasksAsync(cancellationToken);

                await fileSystem.WriteAllTextAsync(
                    jsonlPath, SyncTaskCommand.SerializeJsonl(config, records), cancellationToken);

                await WriteGitIgnoreAsync(fileSystem, gitIgnorePath, force: false, cancellationToken);
            }

            if (console.IsHumanReadable)
            {
                console.OkLine($"Initialized agent workspace at '{AgentWorkspace.DisplayPath}'.");
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
                        + $"from '{Path.GetFileName(jsonlSourcePath)}'.");
                }

                if (hasLegacyTasksDatabase || hasLegacyTasksJsonl)
                {
                    console.WriteLine();
                    console.WriteLine(
                        "The legacy '.nitro/tasks' workspace has been migrated. Remove it from git with:");
                    console.WriteLine("  git rm -r .nitro/tasks");
                    console.WriteLine($"  git add {AgentWorkspace.DisplayPath}");
                }

                if (hasLegacyMailDatabase)
                {
                    console.WriteLine();
                    console.WriteLine(
                        "Found a legacy mail workspace at '.nitro/mail'. It was never released, "
                        + "so its data was not migrated. Delete it with:");
                    console.WriteLine("  rm -rf .nitro/mail");
                }
            }

            return WriteJsonResult(console, resultHolder, workspaceDirectory, prefix, migratedTasks, importedCount);
        }
        catch
        {
            // No partial workspace survives a failed init; the legacy files
            // this reads from are never written to, so the command is
            // retryable afterward.
            if (createdDatabase && fileSystem.FileExists(databasePath))
            {
                fileSystem.DeleteFile(databasePath);
            }

            throw;
        }
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
        int? importedCount)
    {
        if (!console.IsHumanReadable)
        {
            return WriteJsonResult(console, resultHolder, workspaceDirectory, prefix, migratedTasks, importedCount);
        }

        console.OkLine($"Initialized agent workspace at '{AgentWorkspace.DisplayPath}'.");
        console.OkLine($"Task ID prefix set to '{prefix}'.");

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
        string prefix)
    {
        if (!console.IsHumanReadable)
        {
            return WriteJsonResult(
                console, resultHolder, workspaceDirectory, prefix, migratedTasks: 0, importedCount: null);
        }

        console.OkLine(
            $"Upgraded agent workspace schema at '{AgentWorkspace.DisplayPath}' to v{AgentDatabase.CurrentVersion}.");

        return ExitCodes.Success;
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
}
