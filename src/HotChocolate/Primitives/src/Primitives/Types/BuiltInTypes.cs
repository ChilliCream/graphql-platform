using HotChocolate.Language;

namespace HotChocolate.Types;

public static class BuiltInTypes
{
    private static readonly HashSet<string> s_typeNames =
    [
        IntrospectionTypeNames.__Directive,
        IntrospectionTypeNames.__DirectiveLocation,
        IntrospectionTypeNames.__EnumValue,
        IntrospectionTypeNames.__Field,
        IntrospectionTypeNames.__InputValue,
        IntrospectionTypeNames.__Schema,
        IntrospectionTypeNames.__Type,
        IntrospectionTypeNames.__TypeKind,
        SpecScalarNames.String.Name,
        SpecScalarNames.Boolean.Name,
        SpecScalarNames.Float.Name,
        SpecScalarNames.ID.Name,
        SpecScalarNames.Int.Name
    ];

    private static readonly HashSet<string> s_directiveNames =
    [
        DirectiveNames.Skip.Name,
        DirectiveNames.Include.Name,
        DirectiveNames.Deprecated.Name,
        DirectiveNames.Defer.Name,
        DirectiveNames.Stream.Name,
        DirectiveNames.SpecifiedBy.Name,
        DirectiveNames.OneOf.Name
    ];

    public static bool IsBuiltInType(string name)
        => s_typeNames.Contains(name) || s_directiveNames.Contains(name);

    public static DocumentNode RemoveBuiltInTypes(this DocumentNode schema)
    {
        ArgumentNullException.ThrowIfNull(schema);

        var definitions = new List<IDefinitionNode>();

        foreach (var definition in schema.Definitions)
        {
            if (definition is INamedSyntaxNode type)
            {
                if (!s_typeNames.Contains(type.Name.Value))
                {
                    definitions.Add(definition);
                }
            }
            else if (definition is DirectiveDefinitionNode directive)
            {
                if (!s_directiveNames.Contains(directive.Name.Value))
                {
                    definitions.Add(definition);
                }
            }
            else
            {
                definitions.Add(definition);
            }
        }

        return new DocumentNode(definitions);
    }
}
