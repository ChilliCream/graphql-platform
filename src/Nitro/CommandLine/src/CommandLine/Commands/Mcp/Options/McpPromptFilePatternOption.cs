namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Options;

public class McpPromptFilePatternOption: Option<List<string>>
{
    public McpPromptFilePatternOption() : base("--prompt-pattern")
    {
        Description = "One or more file patterns to locate MCP prompt definition files (*.json).";
        IsRequired = false;

        AddAlias("-p");
    }
}
