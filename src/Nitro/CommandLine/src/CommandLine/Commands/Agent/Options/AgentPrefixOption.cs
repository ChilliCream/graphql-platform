namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Options;

internal sealed class AgentPrefixOption : Option<string>
{
    public AgentPrefixOption() : base("--prefix")
    {
        Description = "The task ID prefix (defaults to the current directory name)";
        Required = false;
    }
}
