using System.Collections.Immutable;
using HotChocolate.Fusion.Types.Collections;
using HotChocolate.Fusion.Types.Directives;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Types.Completion;

internal static class CompletionTools
{
    public static DirectiveCollection CreateDirectiveCollection(
        IReadOnlyList<DirectiveNode> directives,
        CompositeSchemaContext context)
    {
        directives = DirectiveTools.GetUserDirectives(directives);

        if(directives.Count == 0)
        {
            return DirectiveCollection.Empty;
        }

        var temp = new CompositeDirective[directives.Count];

        for (var i = 0; i < directives.Count; i++)
        {
            var directive = directives[i];
            var definition = context.GetDirectiveDefinition(directive.Name.Value);
            var arguments = CreateArgumentAssignments(directive.Arguments);
            temp[i] = new CompositeDirective(definition, arguments);
        }

        return new DirectiveCollection(temp);
    }

    private static ArgumentAssignment[] CreateArgumentAssignments(
        IReadOnlyList<ArgumentNode> arguments)
    {
        if(arguments.Count == 0)
        {
            return [];
        }

        var assignments = new ArgumentAssignment[arguments.Count];

        for (var i = 0; i < arguments.Count; i++)
        {
            assignments[i] = CreateArgumentAssignment(arguments[i]);
        }

        return assignments;
    }

    private static ArgumentAssignment CreateArgumentAssignment(
        ArgumentNode argument)
        => new(argument.Name.Value, argument.Value);

    public static CompositeInterfaceTypeCollection CreateInterfaceTypeCollection(
        IReadOnlyList<NamedTypeNode> interfaceTypes,
        CompositeSchemaContext context)
    {
        if(interfaceTypes.Count == 0)
        {
            return CompositeInterfaceTypeCollection.Empty;
        }

        var temp = new CompositeInterfaceType[interfaceTypes.Count];

        for (var i = 0; i < interfaceTypes.Count; i++)
        {
            temp[i] = (CompositeInterfaceType)context.GetType(interfaceTypes[i]);
        }

        return new CompositeInterfaceTypeCollection(temp);
    }

    public static SourceObjectTypeCollection CreateSourceObjectTypeCollection(
        ObjectTypeDefinitionNode typeDef,
        CompositeSchemaContext context)
    {
        var types = TypeDirectiveParser.Parse(typeDef.Directives);
        var lookups = LookupDirectiveParser.Parse(typeDef.Directives);
        var temp = new SourceObjectType[types.Length];

        for (var i = 0; i < types.Length; i++)
        {
            var type = types[i];
            temp[i] = new SourceObjectType(
                typeDef.Name.Value,
                type.SchemaName,
                GetLookupBySchema(lookups, type.SchemaName));
        }

        return new SourceObjectTypeCollection(temp);
    }

    private static ImmutableArray<Lookup> GetLookupBySchema(
        ImmutableArray<LookupDirective> allLookups,
        string schemaName)
    {
        var lookups = ImmutableArray.CreateBuilder<Lookup>();

        foreach (var lookup in allLookups)
        {
            if (lookup.SchemaName.Equals(schemaName, StringComparison.Ordinal))
            {
                var arguments = ImmutableArray.CreateBuilder<LookupArgument>(lookup.Field.Arguments.Count);

                foreach (var argument in lookup.Field.Arguments)
                {
                    arguments.Add(new LookupArgument(argument.Name.Value, argument.Type));
                }

                var fields = ImmutableArray.CreateBuilder<FieldPath>();

                foreach (var field in lookup.Map)
                {
                    fields.Add(FieldPath.Parse(field));
                }

                lookups.Add(
                    new Lookup(
                        lookup.SchemaName,
                        lookup.Field.Name.Value,
                        LookupKind.Default,
                        arguments.ToImmutable(),
                        fields.ToImmutable()));
            }
        }

        return lookups.ToImmutable();
    }
}
