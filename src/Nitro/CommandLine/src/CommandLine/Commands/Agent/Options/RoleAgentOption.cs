namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Options;

internal sealed class RoleAgentOption : Option<string>
{
    public RoleAgentOption() : base("--role")
    {
        Description =
            "The actor role, normalized lowercase. Known roles: orchestrator, planner, implementer, "
            + "reviewer, researcher; any other value is accepted.";
        Required = false;
    }
}
