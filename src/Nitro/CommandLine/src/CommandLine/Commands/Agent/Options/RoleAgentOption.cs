namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Options;

internal sealed class RoleAgentOption : Option<string>
{
    public RoleAgentOption() : base("--role")
    {
        Description = "The agent's role, free text, normalized lowercase (defaults to empty)";
        Required = false;
    }
}
