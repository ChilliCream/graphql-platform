namespace ChilliCream.Nitro.CommandLine.Commands.Init.Mcp.Options;

internal sealed class AgentOption : Option<string>
{
    public AgentOption() : base("--agent")
    {
        Description =
            "The AI agent to configure. Valid values: claude-code, cursor, vscode, windsurf, other";
        IsRequired = false;
    }
}
