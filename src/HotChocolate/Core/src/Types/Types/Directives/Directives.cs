using System.Collections.Generic;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types;

public static class Directives
{
    private static readonly HashSet<NameString> _directiveNames =
        new()
        {
            WellKnownDirectives.Skip,
            WellKnownDirectives.Include,
            WellKnownDirectives.Deprecated,
            WellKnownDirectives.Stream,
            WellKnownDirectives.Defer
        };

    internal static IReadOnlyList<ITypeReference> CreateReferences(
        ITypeInspector typeInspector) =>
        new ITypeReference[]
        {
                typeInspector.GetTypeRef(typeof(SkipDirectiveType)),
                typeInspector.GetTypeRef(typeof(IncludeDirectiveType)),
                typeInspector.GetTypeRef(typeof(DeferDirectiveType)),
                typeInspector.GetTypeRef(typeof(StreamDirectiveType)),
                typeInspector.GetTypeRef(typeof(DeprecatedDirectiveType)),
        };

    public static bool IsBuiltIn(NameString typeName)
    {
        return _directiveNames.Contains(typeName.Value);
    }
}
