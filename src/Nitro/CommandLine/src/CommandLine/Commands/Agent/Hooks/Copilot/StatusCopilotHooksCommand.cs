using ChilliCream.Nitro.CommandLine.Commands.Mail.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Hook;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hooks.Copilot;

/// <summary>
/// Read-only: reports whether each hooks-dir event is missing, installed and
/// current, or outdated.
/// </summary>
internal sealed class StatusCopilotHooksCommand : Command
{
    public StatusCopilotHooksCommand() : base("status")
    {
        Description = "Show whether this CLI's Copilot CLI hook entries are missing, current, or outdated.";

        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples("agent hooks copilot status");

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

        var report = await installer.StatusAsync(cancellationToken);
        var current = report.HooksEvents.All(e => e.Outcome == HookStatusOutcome.Installed);

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(new ObjectResult(ToResult(report, current)));
            return current ? ExitCodes.Success : ExitCodes.Error;
        }

        console.WriteLine($"Copilot CLI hooks in '{report.HooksJsonPath.EscapeMarkup()}':");

        foreach (var eventResult in report.HooksEvents)
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

    private static CopilotHooksStatusResult ToResult(CopilotHooksStatusReport report, bool current) => new(
        report.HooksJsonPath,
        [.. report.HooksEvents.Select(e => new HookEventStatusResult(e.Event, e.Outcome.ToString(), e.InstalledCommand))],
        current);

    public sealed record HookEventStatusResult(string Event, string Outcome, string? InstalledCommand);

    public sealed record CopilotHooksStatusResult(
        string HooksJsonPath, IReadOnlyList<HookEventStatusResult> HooksEvents, bool Current);
}
