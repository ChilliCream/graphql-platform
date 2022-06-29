using System;
using System.Collections.Generic;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types;

/// <summary>
/// Helper class for built-in directives.
/// </summary>
public static class Directives
{
    private static readonly HashSet<string> _directiveNames =
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
        var typeInspector = descriptorContext.TypeInspector;

        if (descriptorContext.Options.EnableOneOf)
        {
            return new ITypeReference[]
            {
                typeInspector.GetTypeRef(typeof(SkipDirectiveType)),
                typeInspector.GetTypeRef(typeof(IncludeDirectiveType)),
                typeInspector.GetTypeRef(typeof(DeferDirectiveType)),
                typeInspector.GetTypeRef(typeof(StreamDirectiveType)),
                typeInspector.GetTypeRef(typeof(DeprecatedDirectiveType)),
                typeInspector.GetTypeRef(typeof(OneOfDirectiveType))
            };
        }

        return new ITypeReference[]
        {
            typeInspector.GetTypeRef(typeof(SkipDirectiveType)),
            typeInspector.GetTypeRef(typeof(IncludeDirectiveType)),
            typeInspector.GetTypeRef(typeof(DeferDirectiveType)),
            typeInspector.GetTypeRef(typeof(StreamDirectiveType)),
            typeInspector.GetTypeRef(typeof(DeprecatedDirectiveType))
        };
    }


    /// <summary>
    /// Checks if the specified directive represents a built-in directive.
    /// </summary>
    public static bool IsBuiltIn(string typeName)
    {
        if (string.IsNullOrEmpty(typeName))
        {
            throw new ArgumentNullException(nameof(typeName));
        }

        return _directiveNames.Contains(typeName);
    }
}
