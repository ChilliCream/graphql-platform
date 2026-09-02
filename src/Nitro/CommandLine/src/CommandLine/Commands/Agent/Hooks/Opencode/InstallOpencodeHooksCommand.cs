using ChilliCream.Nitro.CommandLine.Commands.Agent.Hooks.Options;
using ChilliCream.Nitro.CommandLine.Commands.Agent.Mail.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Hook;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hooks.Opencode;

internal sealed class InstallOpencodeHooksCommand : Command
{
    public InstallOpencodeHooksCommand() : base("install")
    {
        Description = "Add or update Nitro's fail-open Opencode teammate-context plugin.";

        Options.Add(Opt<OpencodeHookInstallScopeOption>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples("agent hooks opencode install", "agent hooks opencode install --scope project");

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
        var report = await installer.InstallAsync(scope, cancellationToken);

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(new ObjectResult(new OpencodeHooksInstallResult(report.Path, report.Outcome.ToString())));
            return ExitCodes.Success;
        }

        console.OkLine($"Installed Nitro's Opencode plugin in '{report.Path.EscapeMarkup()}': {Describe(report.Outcome)}.");
        return ExitCodes.Success;
    }

    private static string Describe(HookInstallOutcome outcome) => outcome switch
    {
        HookInstallOutcome.Installed => "installed",
        HookInstallOutcome.Updated => "updated",
        HookInstallOutcome.Unchanged => "unchanged",
        _ => outcome.ToString()
    };

    public sealed record OpencodeHooksInstallResult(string Path, string Outcome);
}
