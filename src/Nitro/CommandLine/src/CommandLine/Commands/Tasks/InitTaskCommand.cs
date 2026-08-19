using System.Text;
using ChilliCream.Nitro.CommandLine.Commands.Tasks.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Tasks;

namespace ChilliCream.Nitro.CommandLine.Commands.Tasks;

internal sealed class InitTaskCommand : Command
{
    public InitTaskCommand() : base("init")
    {
        Description = "Initialize a task workspace in the current directory.";

        Options.Add(Opt<TaskPrefixOption>.Instance);
        Options.Add(Opt<ForceReinitializeOption>.Instance);

        this.AddExamples("task init", "task init --prefix \"app\"");

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

        var currentDirectory = fileSystem.GetCurrentDirectory();
        var workspaceDirectory = TaskWorkspace.GetDirectory(currentDirectory);
        var databasePath = TaskWorkspace.GetDatabasePath(workspaceDirectory);
        var force = parseResult.GetValue(Opt<ForceReinitializeOption>.Instance);

        if (!force && fileSystem.FileExists(databasePath))
        {
            throw new ExitException(
                $"Already initialized at '{TaskWorkspace.DisplayPath}'. "
                + "Use --force to reinitialize.");
        }

        var prefix = TaskWorkspace.NormalizePrefix(
            parseResult.GetValue(Opt<TaskPrefixOption>.Instance)
                ?? Path.GetFileName(Path.TrimEndingDirectorySeparator(currentDirectory)));

        if (!fileSystem.DirectoryExists(workspaceDirectory))
        {
            fileSystem.CreateDirectory(workspaceDirectory);
        }

        await store.InitializeWorkspaceAsync(workspaceDirectory, prefix, cancellationToken);

        var gitIgnorePath = Path.Combine(
            workspaceDirectory, TaskWorkspace.GitIgnoreFileName);

        if (force || !fileSystem.FileExists(gitIgnorePath))
        {
            await using var gitIgnoreStream = fileSystem.CreateFile(gitIgnorePath);
            await gitIgnoreStream.WriteAsync(
                Encoding.UTF8.GetBytes(TaskWorkspace.GitIgnoreContent),
                cancellationToken);
        }

        console.OkLine($"Initialized task workspace at '{TaskWorkspace.DisplayPath}'.");
        console.OkLine($"Task ID prefix set to '{prefix}'.");

        return ExitCodes.Success;
    }
}
