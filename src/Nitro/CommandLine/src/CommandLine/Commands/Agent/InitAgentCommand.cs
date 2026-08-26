using System.Text;
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
                console, fileSystem, resultHolder, database, currentDirectory, cancellationToken);
        }

        // Location resolution: an initialized workspace anywhere above wins
        // (a .nitro/agents database before the repository's .git/nitro at
        // each level); else, per level, an existing bare .nitro/agents
        // directory (a fresh clone may carry committed memory markdown with
        // no database yet) or the repository's .git/nitro; else a fresh
        // .nitro/agents under the current directory.
        var location = AgentWorkspace.ResolveForInit(fileSystem, currentDirectory);

        var workspaceDirectory = location.WorkspaceDirectory;
        var projectDirectory = location.ProjectDirectory;
        var displayPath = AgentWorkspace.GetDisplayPath(workspaceDirectory);
        var isFallbackLayout = AgentWorkspace.IsFallbackLayout(workspaceDirectory);

        var gitWorkspace = AgentWorkspace.FindGitWorkspace(fileSystem, currentDirectory);
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
            await memoryStore.EnsureProjectWorkspaceAsync(workspaceDirectory, cancellationToken);

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

            await memoryStore.EnsureProjectWorkspaceAsync(workspaceDirectory, cancellationToken);

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

        // Only a .nitro/agents workspace migrates; a workspace already
        // inside a git common directory (this repository's, or an outer
        // repository's in a nested-repo setup) stays where it is.
        if (!AgentWorkspace.IsFallbackLayout(sourceDirectory))
        {
            console.OkLine($"Workspace already at '{sourceDisplay}'; nothing to migrate.");

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
