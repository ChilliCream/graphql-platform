using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types;

/// <summary>
/// Helper class for built-in directives.
/// </summary>
public static class Directives
{
    private static readonly HashSet<string> _directiveNames =
    [
        DirectiveNames.Skip.Name,
        DirectiveNames.Include.Name,
        DirectiveNames.Deprecated.Name,
        DirectiveNames.Stream.Name,
        DirectiveNames.Defer.Name,
        DirectiveNames.OneOf.Name,
        DirectiveNames.SemanticNonNull.Name
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

        if (descriptorContext.Options.EnableSemanticNonNull)
        {
            directiveTypes.Add(typeInspector.GetTypeRef(typeof(SemanticNonNullDirective)));
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
