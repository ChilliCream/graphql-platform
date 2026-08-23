using ChilliCream.Nitro.CommandLine.Commands.Mail.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Hook;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hooks.Copilot;

/// <summary>
/// Removes only this CLI's own hooks-dir entries (semantic restoration, not
/// a byte-accurate restore of the file).
/// </summary>
internal sealed class UninstallCopilotHooksCommand : Command
{
    public UninstallCopilotHooksCommand() : base("uninstall")
    {
        Description = "Remove this CLI's Copilot CLI turn-boundary hook entries.";

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
        var installer = services.GetRequiredService<ICopilotHooksInstallerService>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        var report = await installer.UninstallAsync(cancellationToken);

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(new ObjectResult(ToResult(report)));
            return ExitCodes.Success;
        }

        console.OkLine($"Removed Nitro's Copilot CLI hooks from '{report.HooksJsonPath.EscapeMarkup()}'.");

        foreach (var eventResult in report.HooksEvents)
        {
            console.WriteLine($"  {eventResult.Event,-18}{Describe(eventResult.Outcome)}");
        }

        return ExitCodes.Success;
    }

    private static string Describe(HookUninstallOutcome outcome) => outcome switch
    {
        HookUninstallOutcome.Removed => "removed",
        HookUninstallOutcome.NotPresent => "not present",
        _ => outcome.ToString()
    };

    private static CopilotHooksUninstallResult ToResult(CopilotHooksUninstallReport report) => new(
        report.HooksJsonPath,
        [.. report.HooksEvents.Select(e => new HookEventResult(e.Event, e.Outcome.ToString()))]);

    public sealed record HookEventResult(string Event, string Outcome);

    public sealed record CopilotHooksUninstallResult(
        string HooksJsonPath, IReadOnlyList<HookEventResult> HooksEvents);
}
