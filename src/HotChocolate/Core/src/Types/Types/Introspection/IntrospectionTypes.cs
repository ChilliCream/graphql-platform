using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Introspection;

/// <summary>
/// Helper to identify introspection types.
/// </summary>
public static class IntrospectionTypes
{
    private static readonly HashSet<string> _typeNames =
    [
        __Directive.Names.__Directive,
        __DirectiveLocation.Names.__DirectiveLocation,
        __EnumValue.Names.__EnumValue,
        __Field.Names.__Field,
        __InputValue.Names.__InputValue,
        __Schema.Names.__Schema,
        __Type.Names.__Type,
        __TypeKind.Names.__TypeKind,
        __AppliedDirective.Names.__AppliedDirective,
        __DirectiveArgument.Names.__DirectiveArgument,
    ];

    internal static IReadOnlyList<TypeReference> CreateReferences(
        IDescriptorContext context)
    {
        var types = new List<TypeReference>
            {
                context.TypeInspector.GetTypeRef(typeof(__Directive)),
                context.TypeInspector.GetTypeRef(typeof(__DirectiveLocation)),
                context.TypeInspector.GetTypeRef(typeof(__EnumValue)),
                context.TypeInspector.GetTypeRef(typeof(__Field)),
                context.TypeInspector.GetTypeRef(typeof(__InputValue)),
                context.TypeInspector.GetTypeRef(typeof(__Schema)),
                context.TypeInspector.GetTypeRef(typeof(__Type)),
                context.TypeInspector.GetTypeRef(typeof(__TypeKind)),
            };

        if (context.Options.EnableDirectiveIntrospection)
        {
            types.Add(context.TypeInspector.GetTypeRef(typeof(__AppliedDirective)));
            types.Add(context.TypeInspector.GetTypeRef(typeof(__DirectiveArgument)));
        }

        return types;
    }

    /// <summary>
    /// Defines if the type name represents an introspection type.
    /// </summary>
    public static bool IsIntrospectionType(string typeName)
        => !string.IsNullOrEmpty(typeName)  && _typeNames.Contains(typeName);

    /// <summary>
    /// Defines if the type represents an introspection type.
    /// </summary>
    public static bool IsIntrospectionType(INamedType type)
        => IsIntrospectionType(type.Name);
}
