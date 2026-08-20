using System.Text;
using ChilliCream.Nitro.CommandLine.Commands.Tasks.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Commands.Tasks;

internal sealed class InitTaskCommand : Command
{
    public InitTaskCommand() : base("init")
    {
        Description = "Initialize a task workspace in the current directory.";

        Options.Add(Opt<TaskPrefixOption>.Instance);
        Options.Add(Opt<ForceReinitializeOption>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples("agent tasks init", "agent tasks init --prefix \"app\"");

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

        var currentDirectory = fileSystem.GetCurrentDirectory();
        var workspaceDirectory = AgentWorkspace.GetDirectory(currentDirectory);
        var databasePath = AgentWorkspace.GetDatabasePath(workspaceDirectory);
        var force = parseResult.GetValue(Opt<ForceReinitializeOption>.Instance);

        if (!force && fileSystem.FileExists(databasePath))
        {
            throw new ExitException(
                $"Already initialized at '{AgentWorkspace.DisplayPath}'. "
                + "Use --force to reinitialize.");
        }

        var prefix = AgentWorkspace.NormalizePrefix(
            parseResult.GetValue(Opt<TaskPrefixOption>.Instance)
                ?? Path.GetFileName(Path.TrimEndingDirectorySeparator(currentDirectory)));

        if (!fileSystem.DirectoryExists(workspaceDirectory))
        {
            fileSystem.CreateDirectory(workspaceDirectory);
        }

        await store.InitializeWorkspaceAsync(workspaceDirectory, prefix, cancellationToken);

        var gitIgnorePath = Path.Combine(
            workspaceDirectory, AgentWorkspace.GitIgnoreFileName);

        if (force || !fileSystem.FileExists(gitIgnorePath))
        {
            await using var gitIgnoreStream = fileSystem.CreateFile(gitIgnorePath);
            await gitIgnoreStream.WriteAsync(
                Encoding.UTF8.GetBytes(AgentWorkspace.GitIgnoreContent),
                cancellationToken);
        }

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(new ObjectResult(new TaskWorkspaceInitResult(workspaceDirectory, prefix)));
            return ExitCodes.Success;
        }

        console.OkLine($"Initialized task workspace at '{AgentWorkspace.DisplayPath}'.");
        console.OkLine($"Task ID prefix set to '{prefix}'.");

        return ExitCodes.Success;
    }

    public sealed record TaskWorkspaceInitResult(string Path, string Prefix);
}
