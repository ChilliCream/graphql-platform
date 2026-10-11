using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Hook;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hooks.Claude;

/// <summary>
/// Removes only this CLI's own entries (semantic preservation, not a
/// byte-accurate restore of the whole file): a foreign hook under the same
/// event, or any other content in <c>settings.json</c>, is left exactly as
/// found.
/// </summary>
internal sealed class UninstallClaudeHooksCommand : Command
{
    public UninstallClaudeHooksCommand(ClaudeHooksTemplate template, Option<string> scopeOption) : base("uninstall")
    {
        Description = $"Remove this CLI's {AgentHarnessDisplay.Name(template.Harness)} "
            + "turn-boundary hook entries.";

        Options.Add(scopeOption);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples(
            $"agent hooks {template.CommandName} uninstall",
            $"agent hooks {template.CommandName} uninstall --scope project");

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

        var report = await installer.UninstallAsync(scope, cancellationToken);

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(new ObjectResult(ToResult(report)));
            return ExitCodes.Success;
        }

        console.OkLine($"Removed Nitro's {harnessName} hooks from '{report.SettingsPath.EscapeMarkup()}'.");

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
        [.. report.Events.Select(e => new HookEventUninstallResult(e.Event, e.Outcome.ToString()))]);

    public sealed record HookEventUninstallResult(string Event, string Outcome);

    public sealed record HooksUninstallResult(string SettingsPath, IReadOnlyList<HookEventUninstallResult> Events);
}
