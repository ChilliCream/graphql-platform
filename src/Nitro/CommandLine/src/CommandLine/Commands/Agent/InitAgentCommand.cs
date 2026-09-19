using System.Text;
using ChilliCream.Nitro.CommandLine.Commands.Agent.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using Dapper;
using Microsoft.Data.Sqlite;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent;

/// <summary>
/// Initializes the unified agent workspace: one <c>agents.db</c>, the source
/// of truth shared by the task tracker and mail, at <c>.git/nitro</c> in a
/// git repository or <c>.nitro/agents</c> otherwise (an existing
/// <c>.nitro/agents</c> takes precedence). Also moves a
/// <c>.nitro/agents</c> workspace into <c>.git/nitro</c> with
/// <c>--migrate</c>.
/// </summary>
internal sealed class InitAgentCommand : Command
{
    public InitAgentCommand() : base("init")
    {
        Description = "Initialize an agent workspace in the current directory.";

        Options.Add(Opt<AgentPrefixOption>.Instance);
        Options.Add(Opt<ForceReinitializeAgentOption>.Instance);
        Options.Add(Opt<MigrateAgentOption>.Instance);
        Options.Add(Opt<AgentDatabasePathOption>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        Validators.Add(result =>
        {
            if (result.GetValue(Opt<MigrateAgentOption>.Instance)
                && (result.GetValue(Opt<ForceReinitializeAgentOption>.Instance)
                    || result.GetValue(Opt<AgentPrefixOption>.Instance) is not null))
            {
                result.AddError("'--migrate' cannot be combined with '--force' or '--prefix'.");
            }

            if (result.GetValue(Opt<MigrateAgentOption>.Instance)
                && result.GetValue(Opt<AgentDatabasePathOption>.Instance) is not null)
            {
                result.AddError("'--migrate' cannot be combined with '--database-path'.");
            }
        });

        this.AddExamples(
            "agent init",
            "agent init --prefix \"app\"",
            "agent init --migrate",
            "agent init --database-path \"./.nitro\"");

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
        var resultHolder = services.GetRequiredService<IResultHolder>();
        var database = services.GetRequiredService<AgentDatabase>();

        var currentDirectory = fileSystem.GetCurrentDirectory();

        if (parseResult.GetValue(Opt<MigrateAgentOption>.Instance))
        {
            return await MigrateAsync(
                console, fileSystem, resultHolder, database, currentDirectory, cancellationToken);
        }

        var databasePathOption = parseResult.GetValue(Opt<AgentDatabasePathOption>.Instance);

        WorkspaceLocation location;

        if (databasePathOption is not null)
        {
            // --database-path names a .nitro directory explicitly: the workspace is created at
            // <path>/agents, and resolution never walks up looking for a nearer board.
            location = ResolveForDatabasePathOption(databasePathOption, currentDirectory);
        }
        else
        {
            // Location resolution: an initialized workspace anywhere above wins (a
            // .nitro/agents database before the repository's .git/nitro at each level);
            // else, per level, an existing bare .nitro/agents directory or the
            // repository's .git/nitro; else a fresh .nitro/agents under the current
            // directory.
            location = AgentWorkspace.ResolveForInit(fileSystem, currentDirectory);
        }

        var workspaceDirectory = location.WorkspaceDirectory;
        var projectDirectory = location.ProjectDirectory;
        var displayPath = AgentWorkspace.GetDisplayPath(workspaceDirectory);
        var isFallbackLayout = AgentWorkspace.IsFallbackLayout(workspaceDirectory);

        // --database-path skips the migrate-hint lookup entirely: a board placed there
        // never gets a hint to move it into .git/nitro.
        var gitWorkspace = databasePathOption is null
            ? AgentWorkspace.FindGitWorkspace(fileSystem, currentDirectory)
            : null;
        var migrateAvailable = isFallbackLayout && gitWorkspace is not null;

        var databasePath = AgentWorkspace.GetDatabasePath(workspaceDirectory);
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

            // An existing database at an upgradable schema version: plain init applies
            // the non-destructive schema upgrade only, no prefix or gitignore refresh,
            // instead of throwing. A database newer than this CLI understands still
            // throws here, inside InitializeAsync, regardless of --force.
            await using (await database.InitializeAsync(workspaceDirectory, cancellationToken))
            {
            }

            var upgradedPrefix =
                await ReadPrefixConfigAsync(database, workspaceDirectory, cancellationToken)
                    ?? AgentWorkspace.FallbackPrefix;

            return WriteUpgradeResult(
                console,
                resultHolder,
                workspaceDirectory,
                upgradedPrefix,
                migrateAvailable);
        }

        if (databaseAlreadyExists)
        {
            // --force against an existing unified workspace: reapply the
            // schema in place (non-destructive), refresh the prefix and
            // gitignore.
            var reinitPrefix = AgentWorkspace.NormalizePrefix(explicitPrefix ?? directoryDefaultPrefix);

            await store.InitializeWorkspaceAsync(workspaceDirectory, reinitPrefix, cancellationToken);

            if (isFallbackLayout)
            {
                await WriteGitIgnoreAsync(fileSystem, gitIgnorePath, force: true, cancellationToken);
            }

            return WriteResult(
                console,
                resultHolder,
                workspaceDirectory,
                reinitPrefix,
                migrateAvailable);
        }

        var createdDatabase = false;
        string prefix;

        try
        {
            if (!fileSystem.DirectoryExists(workspaceDirectory))
            {
                fileSystem.CreateDirectory(workspaceDirectory);
            }

            await store.EnsureWorkspaceAsync(workspaceDirectory, cancellationToken);
            createdDatabase = true;

            // Reads/writes the prefix directly against workspaceDirectory, not through
            // ITaskStore's cwd-resolved config methods, which would silently target the
            // wrong board when --database-path names a directory the current directory
            // does not resolve to.
            if (explicitPrefix is not null)
            {
                prefix = AgentWorkspace.NormalizePrefix(explicitPrefix);
            }
            else
            {
                var migratedPrefix =
                    await ReadPrefixConfigAsync(database, workspaceDirectory, cancellationToken);
                prefix = migratedPrefix ?? directoryDefaultPrefix;
            }

            await store.InitializeWorkspaceAsync(workspaceDirectory, prefix, cancellationToken);
        }
        catch
        {
            // No partial workspace survives a failed init, so the command is
            // retryable afterward.
            if (createdDatabase && fileSystem.FileExists(databasePath))
            {
                fileSystem.DeleteFile(databasePath);
            }

            throw;
        }

        if (isFallbackLayout)
        {
            await WriteGitIgnoreAsync(fileSystem, gitIgnorePath, force: false, cancellationToken);
        }

        if (console.IsHumanReadable)
        {
            console.OkLine($"Initialized agent workspace at '{displayPath}'.");
            console.OkLine($"Task ID prefix set to '{prefix}'.");

            if (migrateAvailable)
            {
                WriteMigrateHint(console);
            }
        }

        return WriteJsonResult(console, resultHolder, workspaceDirectory, prefix);
    }

    /// <summary>
    /// Resolves the workspace location for <c>--database-path</c>: the value
    /// names a <c>.nitro</c> directory, relative to <paramref
    /// name="currentDirectory"/> or absolute, and the workspace is created at
    /// <c>&lt;value&gt;/agents</c> (the standard fallback layout), with the
    /// project directory set to the parent of the named <c>.nitro</c>
    /// directory. Rejects a value whose last path segment is not
    /// <c>.nitro</c>.
    /// </summary>
    private static WorkspaceLocation ResolveForDatabasePathOption(
        string databasePathOptionValue,
        string currentDirectory)
    {
        var nitroDirectory = Path.TrimEndingDirectorySeparator(
            Path.GetFullPath(databasePathOptionValue, currentDirectory));

        if (Path.GetFileName(nitroDirectory) != AgentWorkspace.RootDirectoryName)
        {
            throw new ExitException(
                "'--database-path' must name a "
                    + $"'{AgentWorkspace.RootDirectoryName}' directory, got '{databasePathOptionValue}'.");
        }

        var projectDirectory = Path.GetDirectoryName(nitroDirectory) ?? nitroDirectory;
        var workspaceDirectory = Path.Combine(nitroDirectory, AgentWorkspace.AgentsDirectoryName);

        return new WorkspaceLocation(projectDirectory, projectDirectory, workspaceDirectory);
    }

    /// <summary>
    /// Reads the 'prefix' config row directly from the database at
    /// <paramref name="workspaceDirectory"/>, bypassing <see cref="ITaskStore"/>'s
    /// config methods, which connect via the cwd-resolved nearest board
    /// (<see cref="AgentWorkspace.Find"/>) instead. Returns <see langword="null"/> when
    /// no prefix row exists yet.
    /// </summary>
    private static async Task<string?> ReadPrefixConfigAsync(
        AgentDatabase database,
        string workspaceDirectory,
        CancellationToken cancellationToken)
    {
        await using var connection = await database.ConnectAsync(workspaceDirectory, cancellationToken);

        return await connection.QueryFirstOrDefaultAsync<string>(
            "SELECT value FROM config WHERE key = @key",
            new { key = "prefix", cancellationToken });
    }

    /// <summary>
    /// Moves an existing <c>.nitro/agents</c> workspace into the
    /// repository's <c>.git/nitro</c> directory, then applies the schema
    /// upgrade.
    /// </summary>
    private static async Task<int> MigrateAsync(
        INitroConsole console,
        IFileSystem fileSystem,
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

        // Only a .nitro/agents workspace migrates; a workspace already inside a git
        // common directory stays where it is, though it may still be upgraded to the
        // current schema below.
        if (!AgentWorkspace.IsFallbackLayout(sourceDirectory))
        {
            var existingVersion = await database.ReadVersionAsync(sourceDirectory, cancellationToken);

            if (existingVersion == AgentDatabase.CurrentVersion)
            {
                console.OkLine($"Workspace already at '{sourceDisplay}'; nothing to migrate.");

                return WriteMigrateResult(
                    console, resultHolder, sourceDirectory, sourceDirectory);
            }

            // Let InitializeAsync validate the version itself: it rejects a
            // newer or otherwise unsupported version by throwing, so this
            // never reports success for a database it did not actually
            // upgrade.
            await using (await database.InitializeAsync(sourceDirectory, cancellationToken))
            {
            }

            if (console.IsHumanReadable)
            {
                console.OkLine(
                    "Upgraded agent workspace schema at "
                    + $"'{sourceDisplay}' to v{AgentDatabase.CurrentVersion}.");
            }

            return WriteMigrateResult(
                console, resultHolder, sourceDirectory, sourceDirectory);
        }

        if (fileSystem.DirectoryExists(targetDirectory))
        {
            throw new ExitException(
                $"'{targetDisplay}' already exists. Remove it before migrating '{sourceDisplay}'.");
        }

        // Validate and upgrade the schema at the SOURCE, so a database this
        // CLI cannot handle fails the command before anything moves.
        await using (await database.InitializeAsync(sourceDirectory, cancellationToken))
        {
        }

        fileSystem.MoveDirectory(sourceDirectory, targetDirectory);

        // The fallback layout's .gitignore is meaningless inside .git.
        var gitIgnorePath = Path.Combine(targetDirectory, AgentWorkspace.GitIgnoreFileName);

        if (fileSystem.FileExists(gitIgnorePath))
        {
            fileSystem.DeleteFile(gitIgnorePath);
        }

        TryDeleteEmptyNitroRoot(fileSystem, sourceDirectory);

        await UpdateSessionWorkspacePathsAsync(
            targetDirectory, sourceDirectory, cancellationToken);

        if (console.IsHumanReadable)
        {
            console.OkLine($"Moved agent workspace from '{sourceDisplay}' to '{targetDisplay}'.");

            console.WriteLine();
            console.WriteLine($"If '{sourceDisplay}' was committed, remove it from git with:");
            console.WriteLine($"  git rm -r --cached {sourceDisplay}");
        }

        return WriteMigrateResult(console, resultHolder, sourceDirectory, targetDirectory);
    }

    /// <summary>
    /// Deletes the <c>.nitro</c> directory a migrated workspace leaves
    /// behind, but only when it is empty.
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
        catch (UnauthorizedAccessException)
        {
        }
    }

    /// <summary>
    /// Rewrites session rows that recorded the pre-migration workspace path,
    /// so live sessions keep matching the workspace after the move.
    /// </summary>
    private static async Task UpdateSessionWorkspacePathsAsync(
        string workspaceDirectory,
        string previousWorkspaceDirectory,
        CancellationToken cancellationToken)
    {
        var databasePath = AgentWorkspace.GetDatabasePath(workspaceDirectory);

        await using var connection = new SqliteConnection($"Data Source={databasePath};Pooling=False");
        await connection.OpenAsync(cancellationToken);

        await connection.ExecuteAsync(
            "UPDATE agent_sessions SET workspace_path = @workspacePath "
            + "WHERE workspace_path = @previousWorkspacePath;",
            new
            {
                workspacePath = workspaceDirectory,
                previousWorkspacePath = previousWorkspaceDirectory
            });
    }

    private static int WriteMigrateResult(
        INitroConsole console,
        IResultHolder resultHolder,
        string fromDirectory,
        string toDirectory)
    {
        if (console.IsHumanReadable)
        {
            return ExitCodes.Success;
        }

        resultHolder.SetResult(
            new ObjectResult(new AgentWorkspaceMigrateResult(fromDirectory, toDirectory)));

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
        bool migrateAvailable)
    {
        if (!console.IsHumanReadable)
        {
            return WriteJsonResult(console, resultHolder, workspaceDirectory, prefix);
        }

        console.OkLine(
            $"Initialized agent workspace at '{AgentWorkspace.GetDisplayPath(workspaceDirectory)}'.");
        console.OkLine($"Task ID prefix set to '{prefix}'.");

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
        bool migrateAvailable)
    {
        if (!console.IsHumanReadable)
        {
            return WriteJsonResult(console, resultHolder, workspaceDirectory, prefix);
        }

        console.OkLine(
            "Upgraded agent workspace schema at "
            + $"'{AgentWorkspace.GetDisplayPath(workspaceDirectory)}' to v{AgentDatabase.CurrentVersion}.");

        if (migrateAvailable)
        {
            WriteMigrateHint(console);
        }

        return ExitCodes.Success;
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
        string prefix)
    {
        if (console.IsHumanReadable)
        {
            return ExitCodes.Success;
        }

        resultHolder.SetResult(
            new ObjectResult(new AgentWorkspaceInitResult(workspaceDirectory, prefix)));

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

    public sealed record AgentWorkspaceInitResult(string Path, string Prefix);

    public sealed record AgentWorkspaceMigrateResult(string From, string To);
}
