using System;
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
        var typeReferences = new List<ITypeReference>();

        TryIncludeDirectiveType(typeof(SkipDirectiveType));
        TryIncludeDirectiveType(typeof(IncludeDirectiveType));
        TryIncludeDirectiveType(typeof(DeferDirectiveType));
        TryIncludeDirectiveType(typeof(StreamDirectiveType));
        TryIncludeDirectiveType(typeof(DeprecatedDirectiveType));

        if (descriptorContext.Options.EnableOneOf)
        {
            TryIncludeDirectiveType(typeof(OneOfDirectiveType));
        }

        return typeReferences;

        void TryIncludeDirectiveType(Type type)
        {
            if (descriptorContext.Options.IgnoredDirectiveTypes is null || !descriptorContext.Options.IgnoredDirectiveTypes.Contains(type)) {
                typeReferences.Add(typeInspector.GetTypeRef(type));
            };
        }
    }


    /// <summary>
    /// Checks if the specified directive represents a built-in directive.
    /// </summary>
    public static bool IsBuiltIn(NameString typeName)
        => _directiveNames.Contains(typeName.Value);
}
