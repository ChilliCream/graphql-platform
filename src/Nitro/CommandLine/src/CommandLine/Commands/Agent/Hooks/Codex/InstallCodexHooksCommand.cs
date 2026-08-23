using ChilliCream.Nitro.CommandLine.Commands.Mail.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Hook;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hooks.Codex;

/// <summary>
/// Idempotent, same contract as the Claude installer: installing twice
/// writes the same entries, reporting the second run as unchanged. A foreign
/// <c>notify</c> program already configured is wrapped, never silently
/// replaced (install-flow table, Codex row).
/// </summary>
internal sealed class InstallCodexHooksCommand : Command
{
    public InstallCodexHooksCommand() : base("install")
    {
        Description = "Add or update this CLI's Codex CLI turn-boundary hook and notify entries.";

        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples("agent hooks codex install");

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

        var report = await installer.InstallAsync(cancellationToken);

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(new ObjectResult(ToResult(report)));
            return ExitCodes.Success;
        }

        console.OkLine($"Installed Codex CLI hooks in '{report.HooksJsonPath.EscapeMarkup()}'.");

        foreach (var eventResult in report.HooksEvents)
        {
            console.WriteLine($"  {eventResult.Event,-18}{Describe(eventResult.Outcome)}");
        }

        console.WriteLine(
            $"  {"notify",-18}{Describe(report.NotifyOutcome)}"
            + (report.NotifyWrapsForeign ? " (wraps a foreign notify program)" : ""));
        console.WriteLine($"  in '{report.ConfigTomlPath.EscapeMarkup()}'");

        return ExitCodes.Success;
    }

    private static string Describe(HookInstallOutcome outcome) => outcome switch
    {
        HookInstallOutcome.Installed => "installed",
        HookInstallOutcome.Updated => "updated",
        HookInstallOutcome.Unchanged => "unchanged",
        _ => outcome.ToString()
    };

    private static CodexHooksInstallResult ToResult(CodexHooksInstallReport report) => new(
        report.HooksJsonPath,
        [.. report.HooksEvents.Select(e => new HookEventResult(e.Event, e.Outcome.ToString()))],
        report.ConfigTomlPath,
        report.NotifyOutcome.ToString(),
        report.NotifyWrapsForeign);

    public sealed record HookEventResult(string Event, string Outcome);

    public sealed record CodexHooksInstallResult(
        string HooksJsonPath,
        IReadOnlyList<HookEventResult> HooksEvents,
        string ConfigTomlPath,
        string NotifyOutcome,
        bool NotifyWrapsForeign);
}
