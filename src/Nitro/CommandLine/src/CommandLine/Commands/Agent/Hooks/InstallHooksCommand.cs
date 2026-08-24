using ChilliCream.Nitro.CommandLine.Commands.Agent.Hooks.Options;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hooks;

/// <summary>
/// Deprecated alias for <c>agent hooks claude install</c>: same behavior,
/// plus a one-line deprecation notice on stderr. Kept because agents and
/// docs in the wild already call this bare verb; not removed in this ticket.
/// </summary>
internal sealed class InstallHooksCommand : Command
{
    public InstallHooksCommand() : base("install")
    {
        Description = "Add or update this CLI's Claude Code turn-boundary hook entries. "
            + "(deprecated, use hooks claude install)";

        Options.Add(Opt<HookInstallScopeOption>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples("agent hooks install", "agent hooks install --scope project");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        console.Error.MarkupLine(
            "[yellow]'nitro agent hooks install' is deprecated; use 'nitro agent hooks claude install' instead.[/]");

        return Claude.InstallClaudeHooksCommand.ExecuteAsync(services, parseResult, cancellationToken);
    }
}
