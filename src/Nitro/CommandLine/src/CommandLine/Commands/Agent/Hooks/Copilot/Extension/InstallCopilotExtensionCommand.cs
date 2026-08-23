using ChilliCream.Nitro.CommandLine.Commands.Agent.Hooks.Copilot.Extension.Options;
using ChilliCream.Nitro.CommandLine.Commands.Mail.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Hook;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hooks.Copilot.Extension;

/// <summary>
/// Installs the nitro-mail watcher extension asset. <c>--scope</c> is
/// required and only accepts <c>project</c>; there is no user-scope path
/// (see <see cref="CopilotExtensionScopeOption"/>).
/// </summary>
internal sealed class InstallCopilotExtensionCommand : Command
{
    public InstallCopilotExtensionCommand() : base("install")
    {
        Description = "Add or update the nitro-mail Copilot CLI extension asset.";

        Options.Add(Opt<CopilotExtensionScopeOption>.Instance);
        Options.Add(Opt<ForceCopilotExtensionOption>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples("agent hooks copilot extension install --scope project");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var installer = services.GetRequiredService<ICopilotExtensionInstallerService>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        // Parsed but not branched on: the option's AcceptOnlyFromAmong
        // already rejects every value except "project" at parse time, so
        // there is nothing else to resolve here. It stays a required,
        // explicit flag on the command line rather than being dropped
        // entirely, so a caller must always state the scope they mean.
        _ = parseResult.GetRequiredValue(Opt<CopilotExtensionScopeOption>.Instance);
        var force = parseResult.GetValue(Opt<ForceCopilotExtensionOption>.Instance);

        var report = await installer.InstallAsync(force, cancellationToken);

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(new ObjectResult(ToResult(report)));
            return ExitCodes.Success;
        }

        console.OkLine(
            $"{Describe(report.Outcome)} nitro-mail extension at '{report.ExtensionPath.EscapeMarkup()}'.");
        console.WriteLine($"  config: {report.ConfigPath}");

        return ExitCodes.Success;
    }

    private static string Describe(CopilotExtensionInstallOutcome outcome) => outcome switch
    {
        CopilotExtensionInstallOutcome.Installed => "Installed",
        CopilotExtensionInstallOutcome.Updated => "Updated",
        CopilotExtensionInstallOutcome.Unchanged => "Already current:",
        CopilotExtensionInstallOutcome.Forced => "Force-overwrote",
        _ => outcome.ToString()
    };

    private static CopilotExtensionInstallResult ToResult(CopilotExtensionInstallReport report) => new(
        report.ExtensionPath, report.ConfigPath, report.Outcome.ToString());

    public sealed record CopilotExtensionInstallResult(string ExtensionPath, string ConfigPath, string Outcome);
}
