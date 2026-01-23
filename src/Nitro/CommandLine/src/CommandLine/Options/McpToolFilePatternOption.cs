namespace ChilliCream.Nitro.CommandLine.Options;

public class McpToolFilePatternOption: Option<List<string>>
{
    public McpToolFilePatternOption() : base("--tool-patterns")
    {
        Description = "One or more file patterns to locate MCP tool definition files (*.graphql).";
        IsRequired = false;

        AddAlias("-t");
    }
}
