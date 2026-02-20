using HotChocolate.Adapters.Mcp.Directives;
using HotChocolate.Types;
using static HotChocolate.Adapters.Mcp.WellKnownDirectiveNames;

namespace HotChocolate.Adapters.Mcp.Extensions;

internal static class OutputFieldDefinitionExtensions
{
    public static McpToolAnnotationsDirective? GetMcpToolAnnotationsDirective(
        this IOutputFieldDefinition outputField)
    {
        var directive = outputField.Directives[McpToolAnnotations].SingleOrDefault();

        return directive is null ? null : McpToolAnnotationsDirective.From(directive);
    }
}
