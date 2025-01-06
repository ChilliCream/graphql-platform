using System.Collections.Immutable;
using HotChocolate.Fusion.Types.Collections;
using HotChocolate.Fusion.Types.Directives;
using HotChocolate.Language;
using HotChocolate.Fusion.Utilities;

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
            var directiveType = context.GetDirectiveType(directive.Name.Value);
            var arguments = CreateArgumentAssignments(directive.Arguments);
            temp[i] = new CompositeDirective(directiveType, arguments);
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

                var fields = ImmutableArray.CreateBuilder<SelectionPath>();

                foreach (var field in lookup.Map)
                {
                    fields.Add(SelectionPath.Parse(field));
                }

                lookups.Add(
                    new Lookup(
                        lookup.SchemaName,
                        lookup.Field.Name.Value,
                        arguments.ToImmutable(),
                        fields.ToImmutable(),
                        fields.ToSelectionSetNode()));
            }
        }

        return lookups.ToImmutable();
    }
}
