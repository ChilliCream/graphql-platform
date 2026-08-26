using ChilliCream.Nitro.CommandLine.Commands.Agent.Mail.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Hook;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hooks.Codex;

/// <summary>
/// Removes only this CLI's own hooks.json entries and restores any wrapped
/// foreign <c>notify</c> program (semantic restoration, not a byte-accurate
/// restore of either config file).
/// </summary>
internal sealed class UninstallCodexHooksCommand : Command
{
    public UninstallCodexHooksCommand() : base("uninstall")
    {
        Description = "Remove this CLI's Codex CLI turn-boundary hook entries and restore any wrapped "
            + "foreign notify program.";

        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples("agent hooks codex uninstall");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var installer = services.GetRequiredService<ICodexHooksInstallerService>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        var report = await installer.UninstallAsync(cancellationToken);

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(new ObjectResult(ToResult(report)));
            return ExitCodes.Success;
        }

        console.OkLine($"Removed Nitro's Codex CLI hooks from '{report.HooksJsonPath.EscapeMarkup()}'.");

        foreach (var eventResult in report.HooksEvents)
        {
            console.WriteLine($"  {eventResult.Event,-18}{Describe(eventResult.Outcome)}");
        }

        console.WriteLine(
            $"  {"notify",-18}{Describe(report.NotifyOutcome)}"
            + (report.NotifyForeignRestored ? " (restored the prior foreign notify program)" : ""));
        console.WriteLine($"  in '{report.ConfigTomlPath.EscapeMarkup()}'");

        return ExitCodes.Success;
    }

    private static string Describe(HookUninstallOutcome outcome) => outcome switch
    {
        HookUninstallOutcome.Removed => "removed",
        HookUninstallOutcome.NotPresent => "not present",
        _ => outcome.ToString()
    };

    private static CodexHooksUninstallResult ToResult(CodexHooksUninstallReport report) => new(
        report.HooksJsonPath,
        [.. report.HooksEvents.Select(e => new HookEventResult(e.Event, e.Outcome.ToString()))],
        report.ConfigTomlPath,
        report.NotifyOutcome.ToString(),
        report.NotifyForeignRestored);

    public sealed record HookEventResult(string Event, string Outcome);

    public sealed record CodexHooksUninstallResult(
        string HooksJsonPath,
        IReadOnlyList<HookEventResult> HooksEvents,
        string ConfigTomlPath,
        string NotifyOutcome,
        bool NotifyForeignRestored);
}
