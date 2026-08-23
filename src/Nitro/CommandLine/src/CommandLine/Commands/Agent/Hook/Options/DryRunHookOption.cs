namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hook.Options;

internal sealed class DryRunHookOption : Option<bool>
{
    public DryRunHookOption() : base("--dry-run")
    {
        Description = "Resolve the process identity from this process itself instead of walking its "
            + "ancestors for a live Claude Code parent, so a captured payload fixture can drive this "
            + "adapter without a real Claude Code session above it";
        Required = false;
    }
}
