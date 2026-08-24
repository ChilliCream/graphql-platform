using ChilliCream.Nitro.CommandLine.Commands.Agent.Hooks.Options;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hooks;

/// <summary>
/// Deprecated alias for <c>agent hooks claude uninstall</c>: same behavior,
/// plus a one-line deprecation notice on stderr. Kept because agents and
/// docs in the wild already call this bare verb; not removed in this ticket.
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

    private static Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        console.Error.MarkupLine(
            "[yellow]'nitro agent hooks uninstall' is deprecated; use 'nitro agent hooks claude uninstall' instead.[/]");

        return Claude.UninstallClaudeHooksCommand.ExecuteAsync(services, parseResult, cancellationToken);
    }
}
