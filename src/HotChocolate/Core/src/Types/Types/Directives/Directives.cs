using System.Collections.Generic;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types;

/// <summary>
/// Helper class for built-in directives.
/// </summary>
public static class Directives
{
    private static readonly HashSet<NameString> _directiveNames =
        new()
        {
            WellKnownDirectives.Skip,
            WellKnownDirectives.Include,
            WellKnownDirectives.Deprecated,
            WellKnownDirectives.Stream,
            WellKnownDirectives.Defer,
            WellKnownDirectives.OneOf
        };

    internal static IReadOnlyList<ITypeReference> CreateReferences(
        IDescriptorContext descriptorContext)
    {
        ITypeInspector typeInspector = descriptorContext.TypeInspector;
        var typeReferences = new List<ITypeReference>
        {
            typeInspector.GetTypeRef(typeof(SkipDirectiveType)),
            typeInspector.GetTypeRef(typeof(IncludeDirectiveType)),
        };

        if (!descriptorContext.Options.DisableStreamDefer)
        {
            typeReferences.Add(typeInspector.GetTypeRef(typeof(DeferDirectiveType)));
            typeReferences.Add(typeInspector.GetTypeRef(typeof(StreamDirectiveType)));
        }

        typeReferences.Add(typeInspector.GetTypeRef(typeof(DeprecatedDirectiveType)));

        if (descriptorContext.Options.EnableOneOf)
        {
            typeReferences.Add(typeInspector.GetTypeRef(typeof(OneOfDirectiveType)));
            return typeReferences;
        }

        return typeReferences;
    }


    /// <summary>
    /// Checks if the specified directive represents a built-in directive.
    /// </summary>
    public static bool IsBuiltIn(NameString typeName)
        => _directiveNames.Contains(typeName.Value);
}
