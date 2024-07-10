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
    [
        WellKnownDirectives.Skip,
        WellKnownDirectives.Include,
        WellKnownDirectives.Deprecated,
        WellKnownDirectives.Stream,
        WellKnownDirectives.Defer,
        WellKnownDirectives.OneOf,
    ];

    internal static IReadOnlyList<TypeReference> CreateReferences(
        IDescriptorContext descriptorContext)
    {
        var typeInspector = descriptorContext.TypeInspector;
        var directiveTypes = new List<TypeReference>();

        if (descriptorContext.Options.EnableOneOf)
        {
            directiveTypes.Add(typeInspector.GetTypeRef(typeof(OneOfDirectiveType)));
        }

        if (descriptorContext.Options.EnableDefer)
        {
            directiveTypes.Add(typeInspector.GetTypeRef(typeof(DeferDirectiveType)));
        }

        if (descriptorContext.Options.EnableStream)
        {
            directiveTypes.Add(typeInspector.GetTypeRef(typeof(StreamDirectiveType)));
        }
        
        if (descriptorContext.Options.EnableTag)
        {
            directiveTypes.Add(typeInspector.GetTypeRef(typeof(Tag)));
        }

        directiveTypes.Add(typeInspector.GetTypeRef(typeof(SkipDirectiveType)));
        directiveTypes.Add(typeInspector.GetTypeRef(typeof(IncludeDirectiveType)));
        directiveTypes.Add(typeInspector.GetTypeRef(typeof(DeprecatedDirectiveType)));

        return directiveTypes;
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
