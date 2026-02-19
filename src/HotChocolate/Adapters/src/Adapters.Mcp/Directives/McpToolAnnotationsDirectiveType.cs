using HotChocolate.Types;

namespace HotChocolate.Adapters.Mcp.Directives;

/// <summary>
/// Additional properties describing a Tool to clients.
/// </summary>
public sealed class McpToolAnnotationsDirectiveType : DirectiveType<McpToolAnnotationsDirective>
{
    private const string DirectiveName = "mcpToolAnnotations";

    protected override void Configure(
        IDirectiveTypeDescriptor<McpToolAnnotationsDirective> descriptor)
    {
        descriptor
            .Name(DirectiveName)
            .Description("Additional properties describing a Tool to clients.")
            .Location(DirectiveLocation.FieldDefinition)
            .Internal();

        descriptor
            .Argument(d => d.DestructiveHint)
            .Type<BooleanType>()
            .Description(
                "If `true`, the tool may perform destructive updates to its environment. If "
                + "`false`, the tool performs only additive updates.");

        descriptor
            .Argument(d => d.IdempotentHint)
            .Type<BooleanType>()
            .Description(
                "If `true`, calling the tool repeatedly with the same arguments will have no "
                + "additional effect on its environment.");

        descriptor
            .Argument(d => d.OpenWorldHint)
            .Type<BooleanType>()
            .Description(
                "If `true`, this tool may interact with an “open world” of external entities. If "
                + "`false`, the tool’s domain of interaction is closed. For example, the world of "
                + "a web search tool is open, whereas that of a memory tool is not.");
    }
}
