using ChilliCream.Nitro.CommandLine.Commands.Agent.Hooks.Options;
using ChilliCream.Nitro.CommandLine.Commands.Mail.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Hook;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hooks;

/// <summary>
/// Removes only this CLI's own entries (semantic preservation, not a
/// byte-accurate restore of the whole file): a foreign hook under the same
/// event, or any other content in <c>settings.json</c>, is left exactly as
/// found.
/// </summary>
internal sealed class UninstallHooksCommand : Command
{
    public UninstallHooksCommand() : base("uninstall")
    {
        Description = "Remove this CLI's Claude Code turn-boundary hook entries.";

        Options.Add(Opt<HookInstallScopeOption>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples("agent hooks uninstall", "agent hooks uninstall --scope project");

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

        var report = await installer.UninstallAsync(scope, cancellationToken);

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(new ObjectResult(ToResult(report)));
            return ExitCodes.Success;
        }

        console.OkLine($"Removed Nitro's Claude Code hooks from '{report.SettingsPath.EscapeMarkup()}'.");

        foreach (var eventResult in report.Events)
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

    private static HooksUninstallResult ToResult(ClaudeHooksUninstallReport report) => new(
        report.SettingsPath,
        [.. report.Events.Select(e => new HookEventResult(e.Event, e.Outcome.ToString()))]);

    public sealed record HookEventResult(string Event, string Outcome);

    public sealed record HooksUninstallResult(string SettingsPath, IReadOnlyList<HookEventResult> Events);
}
