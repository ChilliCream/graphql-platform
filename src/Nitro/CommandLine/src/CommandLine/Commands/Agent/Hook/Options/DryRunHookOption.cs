namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hook.Options;

internal sealed class DryRunHookOption : Option<bool>
{
    public DryRunHookOption() : base("--dry-run")
    {
        Description = "Pins the row's generation to a fixed sentinel identity (pid 1, epoch proc-start) "
            + "instead of walking ancestors, so captured payload fixtures can drive the adapter. Dry-run "
            + "still writes presence/ledger/budget rows to the real workspace database; do not replay it "
            + "with a live session's session_id";
        Required = false;
    }
}
