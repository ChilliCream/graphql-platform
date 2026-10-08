namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Options;

internal sealed class ForceRestoreAgentOption : Option<bool>
{
    public ForceRestoreAgentOption() : base("--force")
    {
        Description = "Restore without confirmation, even while other agents use the workspace or their activity cannot be checked";
        Required = false;
    }
}
