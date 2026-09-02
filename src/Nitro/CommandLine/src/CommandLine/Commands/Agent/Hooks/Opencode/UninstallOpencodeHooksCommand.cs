using ChilliCream.Nitro.CommandLine.Commands.Agent.Hooks.Options;
using ChilliCream.Nitro.CommandLine.Commands.Agent.Mail.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Hook;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hooks.Opencode;

internal sealed class UninstallOpencodeHooksCommand : Command
{
    public UninstallOpencodeHooksCommand() : base("uninstall")
    {
        Description = "Remove Nitro's Opencode teammate-context plugin.";

        Options.Add(Opt<OpencodeHookInstallScopeOption>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples("agent hooks opencode uninstall", "agent hooks opencode uninstall --scope project");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var installer = services.GetRequiredService<IOpencodeHooksInstallerService>();
        var resultHolder = services.GetRequiredService<IResultHolder>();
        var scope = parseResult.GetRequiredValue(Opt<OpencodeHookInstallScopeOption>.Instance);

        var report = await installer.UninstallAsync(scope, cancellationToken);

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(new ObjectResult(new OpencodeHooksUninstallResult(report.Path, report.Outcome.ToString())));
            return ExitCodes.Success;
        }

        console.OkLine($"Removed Nitro's Opencode plugin from '{report.Path.EscapeMarkup()}': {Describe(report.Outcome)}.");
        return ExitCodes.Success;
    }

    private static string Describe(HookUninstallOutcome outcome) => outcome switch
    {
        HookUninstallOutcome.Removed => "removed",
        HookUninstallOutcome.NotPresent => "not present",
        _ => outcome.ToString()
    };

    public sealed record OpencodeHooksUninstallResult(string Path, string Outcome);
}
