namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Options;

internal sealed class MigrateAgentOption : Option<bool>
{
    public MigrateAgentOption() : base("--migrate")
    {
        Description = "Move an existing .nitro/agents workspace into the repository's .git/nitro directory";
        Required = false;
    }
}
