using ChilliCream.Nitro.CommandLine.Commands.Agent.Mail.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Hook;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hooks.Copilot.Extension;

internal sealed class UninstallCopilotExtensionCommand : Command
{
    public UninstallCopilotExtensionCommand() : base("uninstall")
    {
        Description = "Remove the nitro-mail Copilot CLI extension asset and its config.";

        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples("agent hooks copilot uninstall");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var installer = services.GetRequiredService<ICopilotExtensionInstallerService>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        var report = await installer.UninstallAsync(cancellationToken);

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(new ObjectResult(ToResult(report)));
            return ExitCodes.Success;
        }

        console.OkLine(
            report.Removed
                ? $"Removed the nitro-mail extension from '{report.ExtensionPath.EscapeMarkup()}'."
                : $"No nitro-mail extension was installed at '{report.ExtensionPath.EscapeMarkup()}'.");

        return ExitCodes.Success;
    }

    private static CopilotExtensionUninstallResult ToResult(CopilotExtensionUninstallReport report) => new(
        report.ExtensionPath, report.ConfigPath, report.Removed);

    public sealed record CopilotExtensionUninstallResult(string ExtensionPath, string ConfigPath, bool Removed);
}
