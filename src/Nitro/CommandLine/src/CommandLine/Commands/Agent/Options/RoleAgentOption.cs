namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Options;

internal sealed class RoleAgentOption : Option<string>
{
    public RoleAgentOption() : base("--role")
    {
        Description = "The actor role, normalized lowercase";
        Required = false;
    }
}
