namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Options;

internal sealed class ForceRestoreAgentOption : Option<bool>
{
    public ForceRestoreAgentOption() : base("--force")
    {
        Description = "Restore without confirmation, even while other agents or a mail wake daemon use the workspace";
        Required = false;
    }
}
