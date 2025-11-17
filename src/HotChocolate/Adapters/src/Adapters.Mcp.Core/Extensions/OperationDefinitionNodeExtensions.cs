using HotChocolate.Adapters.Mcp.Directives;
using HotChocolate.Language;

namespace HotChocolate.Adapters.Mcp.Extensions;

internal static class OperationDefinitionNodeExtensions
{
    public static McpToolDirective? GetMcpToolDirective(this OperationDefinitionNode operationNode)
    {
        var mcpToolDirectiveNode =
            operationNode.Directives.FirstOrDefault(d => d.Name.Value == WellKnownDirectiveNames.McpTool);

        return mcpToolDirectiveNode is null
            ? null
            : McpToolDirectiveParser.Parse(mcpToolDirectiveNode);
    }
}
