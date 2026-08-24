using ChilliCream.Nitro.CommandLine.Commands.Agent.Hooks.Options;
using ChilliCream.Nitro.CommandLine.Commands.Mail.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Hook;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hooks.Claude;

/// <summary>
/// Read-only: reports, per managed event, whether a Nitro-owned entry is
/// missing, installed and current, or installed but outdated (its command
/// text differs from what <c>install</c> would write today - a stale launch
/// descriptor after a reinstall in a different mode, or a manual edit, look
/// identical here by design; neither is safe to leave in place unexamined).
/// </summary>
internal sealed class StatusClaudeHooksCommand : Command
{
    public StatusClaudeHooksCommand() : base("status")
    {
        Description = "Show whether this CLI's Claude Code hook entries are missing, current, or outdated.";

        Options.Add(Opt<HookInstallScopeOption>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples("agent hooks claude status", "agent hooks claude status --scope project");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    internal static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var installer = services.GetRequiredService<IClaudeHooksInstallerService>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        var scope = parseResult.GetRequiredValue(Opt<HookInstallScopeOption>.Instance);

        var report = await installer.StatusAsync(scope, cancellationToken);
        var current = report.Events.All(e => e.Outcome == HookStatusOutcome.Installed);

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(new ObjectResult(ToResult(report, current)));
            return current ? ExitCodes.Success : ExitCodes.Error;
        }

        console.WriteLine($"Claude Code hooks in '{report.SettingsPath.EscapeMarkup()}':");

        foreach (var eventResult in report.Events)
        {
            console.WriteLine($"  {eventResult.Event,-18}{Describe(eventResult.Outcome)}");
        }

        return current ? ExitCodes.Success : ExitCodes.Error;
    }

    private static string Describe(HookStatusOutcome outcome) => outcome switch
    {
        HookStatusOutcome.Missing => "missing",
        HookStatusOutcome.Installed => "installed",
        HookStatusOutcome.Outdated => "outdated",
        _ => outcome.ToString()
    };

    private static HooksStatusResult ToResult(ClaudeHooksStatusReport report, bool current) => new(
        report.SettingsPath,
        current,
        [.. report.Events.Select(e => new HookEventStatusResult(e.Event, e.Outcome.ToString(), e.InstalledCommand))]);

    public sealed record HookEventStatusResult(string Event, string Outcome, string? InstalledCommand);

    public sealed record HooksStatusResult(
        string SettingsPath, bool Current, IReadOnlyList<HookEventStatusResult> Events);
}
