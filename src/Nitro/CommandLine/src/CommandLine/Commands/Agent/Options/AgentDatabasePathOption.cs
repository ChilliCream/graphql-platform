namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Options;

internal sealed class AgentDatabasePathOption : Option<string>
{
    public AgentDatabasePathOption() : base("--database-path")
    {
        Description = "Create the workspace in this .nitro directory instead of the nearest existing one";
        Required = false;
    }
}
