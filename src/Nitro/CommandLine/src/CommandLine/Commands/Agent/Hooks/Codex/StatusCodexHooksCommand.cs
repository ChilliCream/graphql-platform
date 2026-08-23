using ChilliCream.Nitro.CommandLine.Commands.Mail.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Hook;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hooks.Codex;

/// <summary>
/// Read-only: reports whether each hooks.json event and the config.toml
/// <c>notify</c> entry are missing, installed and current, or outdated.
/// </summary>
internal sealed class StatusCodexHooksCommand : Command
{
    public StatusCodexHooksCommand() : base("status")
    {
        Description = "Show whether this CLI's Codex CLI hook and notify entries are missing, current, or outdated.";

        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples("agent hooks codex status");

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

        var report = await installer.StatusAsync(cancellationToken);
        var current = report.HooksEvents.All(e => e.Outcome == HookStatusOutcome.Installed)
            && report.NotifyOutcome == HookStatusOutcome.Installed;

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(new ObjectResult(ToResult(report, current)));
            return current ? ExitCodes.Success : ExitCodes.Error;
        }

        console.WriteLine($"Codex CLI hooks in '{report.HooksJsonPath.EscapeMarkup()}':");

        foreach (var eventResult in report.HooksEvents)
        {
            console.WriteLine($"  {eventResult.Event,-18}{Describe(eventResult.Outcome)}");
        }

        console.WriteLine($"Codex CLI notify in '{report.ConfigTomlPath.EscapeMarkup()}':");
        console.WriteLine($"  {"notify",-18}{Describe(report.NotifyOutcome)}");

        return current ? ExitCodes.Success : ExitCodes.Error;
    }

    private static string Describe(HookStatusOutcome outcome) => outcome switch
    {
        HookStatusOutcome.Missing => "missing",
        HookStatusOutcome.Installed => "installed",
        HookStatusOutcome.Outdated => "outdated",
        _ => outcome.ToString()
    };

    private static CodexHooksStatusResult ToResult(CodexHooksStatusReport report, bool current) => new(
        report.HooksJsonPath,
        [.. report.HooksEvents.Select(e => new HookEventStatusResult(e.Event, e.Outcome.ToString(), e.InstalledCommand))],
        report.ConfigTomlPath,
        report.NotifyOutcome.ToString(),
        current);

    public sealed record HookEventStatusResult(string Event, string Outcome, string? InstalledCommand);

    public sealed record CodexHooksStatusResult(
        string HooksJsonPath,
        IReadOnlyList<HookEventStatusResult> HooksEvents,
        string ConfigTomlPath,
        string NotifyOutcome,
        bool Current);
}
