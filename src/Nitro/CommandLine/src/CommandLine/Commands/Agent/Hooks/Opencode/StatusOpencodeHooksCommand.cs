using ChilliCream.Nitro.CommandLine.Commands.Agent.Hooks.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Hook;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hooks.Opencode;

internal sealed class StatusOpencodeHooksCommand : Command
{
    public StatusOpencodeHooksCommand() : base("status")
    {
        Description = "Show whether Nitro's Opencode plugin is missing, current, or outdated.";

        Options.Add(Opt<OpencodeHookInstallScopeOption>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples("agent hooks opencode status", "agent hooks opencode status --scope project");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var installer = services.GetRequiredService<IOpencodeHooksInstallerService>();
        var versionResolver = services.GetRequiredService<IOpencodeVersionResolver>();
        var resultHolder = services.GetRequiredService<IResultHolder>();
        var scope = parseResult.GetRequiredValue(Opt<OpencodeHookInstallScopeOption>.Instance);

        await OpencodeVersionWarning.WriteAsync(console, versionResolver, cancellationToken);
        var report = await installer.StatusAsync(scope, cancellationToken);
        var current = report.Outcome == HookStatusOutcome.Installed;

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(new ObjectResult(new OpencodeHooksStatusResult(
                report.Path,
                report.Outcome.ToString(),
                current)));
            return current ? ExitCodes.Success : ExitCodes.Error;
        }

        console.WriteLine($"Nitro's Opencode plugin in '{report.Path.EscapeMarkup()}': {Describe(report.Outcome)}.");
        return current ? ExitCodes.Success : ExitCodes.Error;
    }

    private static string Describe(HookStatusOutcome outcome) => outcome switch
    {
        HookStatusOutcome.Missing => "missing",
        HookStatusOutcome.Installed => "installed",
        HookStatusOutcome.Outdated => "outdated",
        _ => outcome.ToString()
    };

    public sealed record OpencodeHooksStatusResult(string Path, string Outcome, bool Current);
}
