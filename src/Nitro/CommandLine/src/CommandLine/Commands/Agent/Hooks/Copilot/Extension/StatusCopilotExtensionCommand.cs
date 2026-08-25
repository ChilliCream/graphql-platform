using ChilliCream.Nitro.CommandLine.Commands.Mail.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Hook;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hooks.Copilot.Extension;

internal sealed class StatusCopilotExtensionCommand : Command
{
    public StatusCopilotExtensionCommand() : base("status")
    {
        Description = "Show whether the nitro-mail Copilot CLI extension asset is missing, current, outdated, or unrecognized.";

        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples("agent hooks copilot status");

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

        var report = await installer.StatusAsync(cancellationToken);
        var current = report.Outcome == CopilotExtensionStatusOutcome.Current;

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(new ObjectResult(ToResult(report)));
            return current ? ExitCodes.Success : ExitCodes.Error;
        }

        console.WriteLine($"nitro-mail extension at '{report.ExtensionPath.EscapeMarkup()}': {Describe(report.Outcome)}");

        return current ? ExitCodes.Success : ExitCodes.Error;
    }

    private static string Describe(CopilotExtensionStatusOutcome outcome) => outcome switch
    {
        CopilotExtensionStatusOutcome.Missing => "missing",
        CopilotExtensionStatusOutcome.Current => "current",
        CopilotExtensionStatusOutcome.Outdated => "outdated",
        CopilotExtensionStatusOutcome.Unrecognized => "unrecognized (not a Nitro-installed version)",
        _ => outcome.ToString()
    };

    private static CopilotExtensionStatusResult ToResult(CopilotExtensionStatusReport report) => new(
        report.ExtensionPath, report.ConfigPath, report.Outcome.ToString());

    public sealed record CopilotExtensionStatusResult(string ExtensionPath, string ConfigPath, string Outcome);
}
