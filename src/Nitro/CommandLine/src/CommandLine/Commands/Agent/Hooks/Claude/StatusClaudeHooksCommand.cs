using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Hook;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hooks.Claude;

/// <summary>
/// Read-only: reports, per managed event, whether a Nitro-owned entry is missing,
/// installed and current, installed but outdated (it differs from what <c>install</c>
/// would write today), or switched off by the harness's own <c>enabled</c> flag.
/// </summary>
internal sealed class StatusClaudeHooksCommand : Command
{
    public StatusClaudeHooksCommand(ClaudeHooksTemplate template, Option<string> scopeOption) : base("status")
    {
        Description = $"Show whether this CLI's {AgentHarnessDisplay.Name(template.Harness)} "
            + "hook entries are missing, current, or outdated.";

        Options.Add(scopeOption);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples(
            $"agent hooks {template.CommandName} status",
            $"agent hooks {template.CommandName} status --scope project");

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

        var report = await installer.StatusAsync(scope, cancellationToken);
        var current = report.Events.All(e => e.Outcome == HookStatusOutcome.Installed)
            && report.AllHooksDisabledIn is null;

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(new ObjectResult(ToResult(report, current)));
            return current ? ExitCodes.Success : ExitCodes.Error;
        }

        console.WriteLine($"{harnessName} hooks in '{report.SettingsPath.EscapeMarkup()}':");

        foreach (var eventResult in report.Events)
        {
            console.WriteLine($"  {eventResult.Event,-18}{Describe(eventResult.Outcome)}");
        }

        HookSwitchGuidance.Write(
            console,
            harnessName,
            report.SettingsPath,
            [.. report.Events.Where(e => e.Outcome == HookStatusOutcome.Disabled).Select(e => e.Event)],
            report.AllHooksDisabledIn);

        return current ? ExitCodes.Success : ExitCodes.Error;
    }

    private static string Describe(HookStatusOutcome outcome) => outcome switch
    {
        HookStatusOutcome.Missing => "missing",
        HookStatusOutcome.Installed => "installed",
        HookStatusOutcome.Outdated => "outdated",
        HookStatusOutcome.Disabled => "disabled",
        _ => outcome.ToString()
    };

    private static HooksStatusResult ToResult(ClaudeHooksStatusReport report, bool current) => new(
        report.SettingsPath,
        current,
        [.. report.Events.Select(e => new HookEventStatusResult(e.Event, e.Outcome.ToString(), e.InstalledCommand))],
        report.AllHooksDisabledIn);

    public sealed record HookEventStatusResult(string Event, string Outcome, string? InstalledCommand);

    public sealed record HooksStatusResult(
        string SettingsPath,
        bool Current,
        IReadOnlyList<HookEventStatusResult> Events,
        string? AllHooksDisabledIn);
}
