using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Hook;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hooks.Claude;

/// <summary>
/// Idempotent: installing twice writes the same entries, reporting the
/// second run as unchanged. Leaves exactly one Nitro-owned hook handler per
/// event; foreign handlers (another tool's hook under the same event, even in
/// the same group) are left untouched.
/// </summary>
internal sealed class InstallClaudeHooksCommand : Command
{
    public InstallClaudeHooksCommand(ClaudeHooksTemplate template, Option<string> scopeOption) : base("install")
    {
        Description = $"Add or update this CLI's {AgentHarnessDisplay.Name(template.Harness)} "
            + "turn-boundary hook entries.";

        Options.Add(scopeOption);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples(
            $"agent hooks {template.CommandName} install",
            $"agent hooks {template.CommandName} install --scope project");

        this.SetActionWithExceptionHandling(
            (services, parseResult, cancellationToken) =>
                ExecuteAsync(services, parseResult, template, scopeOption, cancellationToken));
    }

    internal static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        ClaudeHooksTemplate template,
        Option<string> scopeOption,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var installer = services.GetRequiredKeyedService<IClaudeHooksInstallerService>(template.Harness);
        var resultHolder = services.GetRequiredService<IResultHolder>();

        var scope = parseResult.GetRequiredValue(scopeOption);
        var harnessName = AgentHarnessDisplay.Name(template.Harness);

        var report = await installer.InstallAsync(scope, cancellationToken);

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(new ObjectResult(ToResult(report)));
            return ExitCodes.Success;
        }

        console.OkLine($"Installed {harnessName} hooks in '{report.SettingsPath.EscapeMarkup()}'.");

        foreach (var eventResult in report.Events)
        {
            console.WriteLine($"  {eventResult.Event,-18}{Describe(eventResult.Outcome)}");
        }

        HookSwitchGuidance.Write(
            console,
            harnessName,
            report.SettingsPath,
            [.. report.Events.Where(e => e.Outcome == HookInstallOutcome.Disabled).Select(e => e.Event)],
            report.AllHooksDisabledIn);

        return ExitCodes.Success;
    }

    private static string Describe(HookInstallOutcome outcome) => outcome switch
    {
        HookInstallOutcome.Installed => "installed",
        HookInstallOutcome.Updated => "updated",
        HookInstallOutcome.Unchanged => "unchanged",
        HookInstallOutcome.Disabled => "disabled",
        _ => outcome.ToString()
    };

    private static HooksInstallResult ToResult(ClaudeHooksInstallReport report) => new(
        report.SettingsPath,
        [.. report.Events.Select(e => new HookEventInstallResult(e.Event, e.Outcome.ToString()))],
        report.AllHooksDisabledIn);

    public sealed record HookEventInstallResult(string Event, string Outcome);

    public sealed record HooksInstallResult(
        string SettingsPath, IReadOnlyList<HookEventInstallResult> Events, string? AllHooksDisabledIn);
}
