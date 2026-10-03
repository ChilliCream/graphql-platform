using ChilliCream.Nitro.CommandLine.Commands.Agent.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent;

/// <summary>
/// Replaces the project's <c>.nitro</c> directory and the repository's <c>.git/nitro</c>
/// directory with the contents of a workspace archive written by <c>agent backup</c>.
/// </summary>
internal sealed class RestoreAgentCommand : Command
{
    public RestoreAgentCommand() : base("restore")
    {
        Description = "Replace the agent workspace with a backup archive. "
            + "Deletes '.nitro' and '.git/nitro' first.";

        Options.Add(Opt<ArchiveAgentOption>.Instance);
        Options.Add(Opt<RestoreActorOption>.Instance);
        Options.Add(Opt<ForceRestoreAgentOption>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples(
            "agent restore --archive \"./nitro-backup.zip\"",
            "agent restore --archive \"./nitro-backup.zip\" --actor \"maya\"",
            "agent restore --archive \"./nitro-backup.zip\" --force");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var fileSystem = services.GetRequiredService<IFileSystem>();
        var timeProvider = services.GetRequiredService<TimeProvider>();
        var resultHolder = services.GetRequiredService<IResultHolder>();
        var actorResolver = services.GetRequiredService<IActingActorResolver>();

        var currentDirectory = fileSystem.GetCurrentDirectory();
        var archivePath = Path.GetFullPath(
            parseResult.GetRequiredValue(Opt<ArchiveAgentOption>.Instance), currentDirectory);
        var force = parseResult.GetValue(Opt<ForceRestoreAgentOption>.Instance);

        var (projectDirectory, gitWorkspaceDirectory) =
            AgentWorkspaceArchive.ResolveDirectories(fileSystem, currentDirectory);

        // Everything that can reject the archive runs before anything is asked or deleted.
        AgentWorkspaceArchive.Validate(archivePath, projectDirectory, gitWorkspaceDirectory);

        var activity = await AgentWorkspaceActivity.InspectAsync(
            GetWorkspaceDirectories(projectDirectory, gitWorkspaceDirectory),
            timeProvider.GetUtcNow(),
            cancellationToken);

        if (activity.ActiveMailWakeLease is { } lease)
        {
            throw ThrowHelper.MailWakeDaemonBlocksRestore(lease.WorkspaceDirectory, lease.ExpiresAt);
        }

        if (!force)
        {
            if (activity.UnreadableDatabases.Count > 0)
            {
                throw ThrowHelper.RestoreActivityUnknown(activity.UnreadableDatabases);
            }

            var blockingAgents = activity.ActiveAgents;

            if (blockingAgents.Count > 0
                && parseResult.GetValue(Opt<RestoreActorOption>.Instance) is { } actorOption)
            {
                var actor = await actorResolver.ResolveAsync(actorOption, cancellationToken);

                blockingAgents = blockingAgents.Where(agent => agent != actor).ToArray();
            }

            if (blockingAgents.Count > 0)
            {
                throw ThrowHelper.ActiveAgentsBlockRestore(blockingAgents);
            }

            if (!console.IsInteractive)
            {
                throw ThrowHelper.RestoreRequiresForce();
            }

            var directories = gitWorkspaceDirectory is null
                ? $"'{projectDirectory}'"
                : $"'{projectDirectory}' and '{gitWorkspaceDirectory}'";

            var confirmed = await console.ConfirmAsync(
                $"Delete {directories.EscapeMarkup()} and restore from '{archivePath.EscapeMarkup()}'?",
                cancellationToken);

            if (!confirmed)
            {
                console.WriteLine("Aborted.");
                return ExitCodes.Success;
            }
        }

        var summary = await AgentWorkspaceArchive.RestoreAsync(
            archivePath,
            projectDirectory,
            gitWorkspaceDirectory,
            cancellationToken);

        if (console.IsHumanReadable)
        {
            var files = summary.FileCount == 1 ? "file" : "files";

            console.OkLine(
                $"Restored {summary.FileCount} {files} from '{summary.Archive.EscapeMarkup()}'.");

            foreach (var directory in summary.LeftoverDirectories)
            {
                console.MarkupLine(
                    $"Could not delete the replaced folder '{directory.EscapeMarkup()}'. Remove it manually."
                        .AsWarning());
            }

            return ExitCodes.Success;
        }

        resultHolder.SetResult(new ObjectResult(summary));

        return ExitCodes.Success;
    }

    private static IEnumerable<string> GetWorkspaceDirectories(
        string projectDirectory,
        string? gitWorkspaceDirectory)
    {
        yield return Path.Combine(projectDirectory, AgentWorkspace.AgentsDirectoryName);

        if (gitWorkspaceDirectory is not null)
        {
            yield return gitWorkspaceDirectory;
        }
    }
}
