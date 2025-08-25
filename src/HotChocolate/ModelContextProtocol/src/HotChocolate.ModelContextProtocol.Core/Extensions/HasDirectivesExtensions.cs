using HotChocolate.Language;
using HotChocolate.Types;
using static HotChocolate.ModelContextProtocol.WellKnownArgumentNames;
using static HotChocolate.ModelContextProtocol.WellKnownDirectiveNames;

namespace HotChocolate.ModelContextProtocol.Extensions;

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
