namespace ChilliCream.Nitro.CommandLine.Options;

public class McpPromptFilePatternOption: Option<List<string>>
{
    public McpPromptFilePatternOption() : base("--prompt-patterns")
    {
        Description = "One or more file patterns to locate MCP prompt definition files (*.json).";
        IsRequired = false;

        AddAlias("-p");
    }
}
