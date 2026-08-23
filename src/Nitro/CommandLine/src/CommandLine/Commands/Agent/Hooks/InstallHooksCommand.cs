using ChilliCream.Nitro.CommandLine.Commands.Agent.Hooks.Options;
using ChilliCream.Nitro.CommandLine.Commands.Mail.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Hook;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hooks;

/// <summary>
/// Idempotent: installing twice writes the same entries, reporting the
/// second run as unchanged. Only ever adds or replaces the single
/// Nitro-owned hook group per event; foreign entries (another tool's hook
/// under the same event) are left untouched.
/// </summary>
internal sealed class InstallHooksCommand : Command
{
    public InstallHooksCommand() : base("install")
    {
        Description = "Add or update this CLI's Claude Code turn-boundary hook entries.";

        Options.Add(Opt<HookInstallScopeOption>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples("agent hooks install", "agent hooks install --scope project");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var installer = services.GetRequiredService<IClaudeHooksInstallerService>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        var scope = parseResult.GetRequiredValue(Opt<HookInstallScopeOption>.Instance);

        var report = await installer.InstallAsync(scope, cancellationToken);

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(new ObjectResult(ToResult(report)));
            return ExitCodes.Success;
        }

        console.OkLine($"Installed Claude Code hooks in '{report.SettingsPath.EscapeMarkup()}'.");

        foreach (var eventResult in report.Events)
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

    private static HooksInstallResult ToResult(ClaudeHooksInstallReport report) => new(
        report.SettingsPath,
        [.. report.Events.Select(e => new HookEventResult(e.Event, e.Outcome.ToString()))]);

    public sealed record HookEventResult(string Event, string Outcome);

    public sealed record HooksInstallResult(string SettingsPath, IReadOnlyList<HookEventResult> Events);
}
