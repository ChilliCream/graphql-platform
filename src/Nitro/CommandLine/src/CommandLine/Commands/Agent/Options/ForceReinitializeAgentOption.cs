namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Options;

internal sealed class ForceReinitializeAgentOption : Option<bool>
{
    public ForceReinitializeAgentOption() : base("--force")
    {
        Description = "Reinitialize an existing agent workspace";
        Required = false;
    }
}
