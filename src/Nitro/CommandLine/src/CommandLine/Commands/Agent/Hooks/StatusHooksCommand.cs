using ChilliCream.Nitro.CommandLine.Commands.Agent.Hooks.Options;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hooks;

/// <summary>
/// Deprecated alias for <c>agent hooks claude status</c>: same behavior,
/// plus a one-line deprecation notice on stderr. Kept because agents and
/// docs in the wild already call this bare verb; not removed in this ticket.
/// </summary>
internal sealed class StatusHooksCommand : Command
{
    public StatusHooksCommand() : base("status")
    {
        Description = "Show whether this CLI's Claude Code hook entries are missing, current, or outdated. "
            + "(deprecated, use hooks claude status)";

        Options.Add(Opt<HookInstallScopeOption>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples("agent hooks status", "agent hooks status --scope project");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        console.Error.MarkupLine(
            "[yellow]'nitro agent hooks status' is deprecated; use 'nitro agent hooks claude status' instead.[/]");

        return Claude.StatusClaudeHooksCommand.ExecuteAsync(services, parseResult, cancellationToken);
    }
}
