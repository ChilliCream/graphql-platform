namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Options;

internal sealed class CleanMixedInstanceAgentOption : Option<bool>
{
    public CleanMixedInstanceAgentOption() : base("--clean-mixed-instance")
    {
        Description = "Delete session rows stranded from a previous Nitro instance id "
            + "(a regenerated fallback id, or a different host sharing this workspace); "
            + "these rows are never reaped automatically";
        Required = false;
    }
}
