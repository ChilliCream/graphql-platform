using HotChocolate.ModelContextProtocol.Directives;
using HotChocolate.Types;
using static HotChocolate.ModelContextProtocol.WellKnownDirectiveNames;

namespace HotChocolate.ModelContextProtocol.Extensions;

internal static class OutputFieldDefinitionExtensions
{
    public static McpToolAnnotationsDirective? GetMcpToolAnnotationsDirective(
        this IOutputFieldDefinition outputField)
    {
        var directive = outputField.Directives[McpToolAnnotations].SingleOrDefault();

        return directive?.ToValue<McpToolAnnotationsDirective>();
    }
}
