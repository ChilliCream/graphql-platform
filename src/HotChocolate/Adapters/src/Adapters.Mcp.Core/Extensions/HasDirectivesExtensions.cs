using HotChocolate.Language;
using HotChocolate.Types;
using static HotChocolate.Adapters.Mcp.WellKnownArgumentNames;
using static HotChocolate.Adapters.Mcp.WellKnownDirectiveNames;

namespace HotChocolate.Adapters.Mcp.Extensions;

internal static class HasDirectivesExtensions
{
    public static object? GetSkipIfValue(this IHasDirectives type)
    {
        return type.Directives.SingleOrDefault(d => d.Name.Value == Skip)?.GetArgumentValue(If)?.Value;
    }

    public static object? GetIncludeIfValue(this IHasDirectives type)
    {
        return type.Directives.SingleOrDefault(d => d.Name.Value == Include)?.GetArgumentValue(If)?.Value;
    }
}
