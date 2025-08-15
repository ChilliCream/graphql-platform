using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.ModelContextProtocol.Directives;

namespace HotChocolate.ModelContextProtocol.Extensions;

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

    public static bool TryGetMcpToolDirective(
        this OperationDefinitionNode operationNode,
        [NotNullWhen(true)] out McpToolDirective? mcpToolDirective)
    {
        mcpToolDirective = operationNode.GetMcpToolDirective();

        return mcpToolDirective is not null;
    }
}
