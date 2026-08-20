using System.Text;
using ChilliCream.Nitro.CommandLine.Commands.Mail.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Commands.Mail;

internal sealed class InitMailCommand : Command
{
    public InitMailCommand() : base("init")
    {
        Description = "Initialize a mail workspace in the current directory.";

        Options.Add(Opt<ForceReinitializeMailOption>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples("agent mail init", "agent mail init --force");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var fileSystem = services.GetRequiredService<IFileSystem>();
        var store = services.GetRequiredService<IMailStore>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        var currentDirectory = fileSystem.GetCurrentDirectory();
        var workspaceDirectory = AgentWorkspace.GetDirectory(currentDirectory);
        var databasePath = AgentWorkspace.GetDatabasePath(workspaceDirectory);
        var force = parseResult.GetValue(Opt<ForceReinitializeMailOption>.Instance);

        if (!force && fileSystem.FileExists(databasePath))
        {
            throw new ExitException(
                $"Already initialized at '{AgentWorkspace.DisplayPath}'. "
                + "Use --force to reinitialize.");
        }

        if (!fileSystem.DirectoryExists(workspaceDirectory))
        {
            fileSystem.CreateDirectory(workspaceDirectory);
        }

        await store.InitializeWorkspaceAsync(workspaceDirectory, cancellationToken);

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
            resultHolder.SetResult(new ObjectResult(new MailWorkspaceInitResult(workspaceDirectory)));
            return ExitCodes.Success;
        }

        console.OkLine($"Initialized mail workspace at '{AgentWorkspace.DisplayPath}'.");

        return ExitCodes.Success;
    }

    public sealed record MailWorkspaceInitResult(string Path);
}
