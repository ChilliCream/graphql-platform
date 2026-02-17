namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Options;

public class McpToolFilePatternOption : Option<List<string>>
{
    public McpToolFilePatternOption() : base("--tool-pattern")
    {
        Description = "One or more file patterns to locate MCP tool definition files (*.graphql).";
        IsRequired = false;

        AddAlias("-t");
    }
}
