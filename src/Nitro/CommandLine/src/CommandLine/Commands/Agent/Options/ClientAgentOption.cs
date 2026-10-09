namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Options;

internal sealed class ClientAgentOption : Option<string>
{
    public ClientAgentOption() : base("--client")
    {
        Description = "The client program the agent runs as, e.g. \"claude-code\" or \"codex\", "
            + "free text, normalized lowercase (defaults to auto-detected, or empty)";
        Required = false;
    }
}
