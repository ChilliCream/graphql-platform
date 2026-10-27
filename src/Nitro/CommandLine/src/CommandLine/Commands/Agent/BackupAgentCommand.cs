using ChilliCream.Nitro.CommandLine.Commands.Agent.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent;

/// <summary>
/// Packs the project's <c>.nitro</c> directory and the repository's <c>.git/nitro</c>
/// directory into one zip archive.
/// </summary>
internal sealed class BackupAgentCommand : Command
{
    public BackupAgentCommand() : base("backup")
    {
        Description = "Back up the agent workspace to a zip archive.";

        Options.Add(Opt<ArchiveAgentOption>.Instance);
        Options.Add(Opt<ForceBackupAgentOption>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples(
            "agent backup --archive \"./nitro-backup.zip\"",
            "agent backup --archive \"./nitro-backup.zip\" --force");

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

        var currentDirectory = fileSystem.GetCurrentDirectory();
        var archivePath = Path.GetFullPath(
            parseResult.GetRequiredValue(Opt<ArchiveAgentOption>.Instance), currentDirectory);
        var force = parseResult.GetValue(Opt<ForceBackupAgentOption>.Instance);

        var (projectDirectory, gitWorkspaceDirectory) = ResolveDirectories(fileSystem, currentDirectory);

        if (projectDirectory is null && gitWorkspaceDirectory is null)
        {
            throw ThrowHelper.NoAgentWorkspaceToBackUp();
        }

        var summary = await AgentWorkspaceArchive.WriteAsync(
            archivePath,
            projectDirectory,
            gitWorkspaceDirectory,
            force,
            timeProvider,
            cancellationToken);

        if (console.IsHumanReadable)
        {
            var roots = string.Join(" and ", summary.Roots.Select(root => $"'{root}'"));
            var files = summary.FileCount == 1 ? "file" : "files";

            console.OkLine($"Backed up {summary.FileCount} {files} from {roots} to '{summary.Archive.EscapeMarkup()}'.");

            return ExitCodes.Success;
        }

        resultHolder.SetResult(new ObjectResult(summary));

        return ExitCodes.Success;
    }

    /// <summary>
    /// Resolves the project <c>.nitro</c> directory and the git workspace directory to back
    /// up, each null when it does not exist.
    /// </summary>
    private static (string? ProjectDirectory, string? GitWorkspaceDirectory) ResolveDirectories(
        IFileSystem fileSystem,
        string currentDirectory)
    {
        var (projectDirectory, gitWorkspaceDirectory) =
            AgentWorkspaceArchive.ResolveDirectories(fileSystem, currentDirectory);

        return (
            fileSystem.DirectoryExists(projectDirectory) ? projectDirectory : null,
            gitWorkspaceDirectory is not null && fileSystem.DirectoryExists(gitWorkspaceDirectory)
                ? gitWorkspaceDirectory
                : null);
    }
}
