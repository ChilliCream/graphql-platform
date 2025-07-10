using HotChocolate.Language;

namespace HotChocolate.Utilities.Introspection;

public static class BuiltInTypes
{
    private static readonly HashSet<string> s_typeNames =
    [
        WellKnownTypes.__Directive,
        WellKnownTypes.__DirectiveLocation,
        WellKnownTypes.__EnumValue,
        WellKnownTypes.__Field,
        WellKnownTypes.__InputValue,
        WellKnownTypes.__Schema,
        WellKnownTypes.__Type,
        WellKnownTypes.__TypeKind,
        WellKnownTypes.String,
        WellKnownTypes.Boolean,
        WellKnownTypes.Float,
        WellKnownTypes.ID,
        WellKnownTypes.Int
    ];

    private static readonly HashSet<string> s_directiveNames =
    [
        WellKnownDirectives.Skip,
        WellKnownDirectives.Include,
        WellKnownDirectives.Deprecated,
        WellKnownDirectives.Defer,
        WellKnownDirectives.Stream,
        WellKnownDirectives.SpecifiedBy
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
