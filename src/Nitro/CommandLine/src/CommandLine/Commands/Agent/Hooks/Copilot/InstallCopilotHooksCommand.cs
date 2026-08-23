using ChilliCream.Nitro.CommandLine.Commands.Mail.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Hook;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hooks.Copilot;

/// <summary>
/// Idempotent, same contract as the Claude and Codex installers: installing
/// twice writes the same entries, reporting the second run as unchanged.
/// </summary>
internal sealed class InstallCopilotHooksCommand : Command
{
    public InstallCopilotHooksCommand() : base("install")
    {
        Description = "Add or update this CLI's Copilot CLI turn-boundary hook entries.";

        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples("agent hooks copilot install");

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

        var report = await installer.InstallAsync(cancellationToken);

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(new ObjectResult(ToResult(report)));
            return ExitCodes.Success;
        }

        console.OkLine($"Installed Copilot CLI hooks in '{report.HooksJsonPath.EscapeMarkup()}'.");

        foreach (var eventResult in report.HooksEvents)
        {
            console.WriteLine($"  {eventResult.Event,-18}{Describe(eventResult.Outcome)}");
        }

        return ExitCodes.Success;
    }

    private static string Describe(HookInstallOutcome outcome) => outcome switch
    {
        HookInstallOutcome.Installed => "installed",
        HookInstallOutcome.Updated => "updated",
        HookInstallOutcome.Unchanged => "unchanged",
        _ => outcome.ToString()
    };

    private static CopilotHooksInstallResult ToResult(CopilotHooksInstallReport report) => new(
        report.HooksJsonPath,
        [.. report.HooksEvents.Select(e => new HookEventResult(e.Event, e.Outcome.ToString()))]);

    public sealed record HookEventResult(string Event, string Outcome);

    public sealed record CopilotHooksInstallResult(
        string HooksJsonPath, IReadOnlyList<HookEventResult> HooksEvents);
}
