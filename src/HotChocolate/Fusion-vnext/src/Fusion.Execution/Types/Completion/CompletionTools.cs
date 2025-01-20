using System.Collections.Immutable;
using HotChocolate.Fusion.Types.Collections;
using HotChocolate.Fusion.Types.Directives;
using HotChocolate.Fusion.Utilities;
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

    public static SourceInterfaceTypeCollection CreateSourceInterfaceTypeCollection(
        InterfaceTypeDefinitionNode typeDef,
        CompositeSchemaContext context)
    {
        var types = TypeDirectiveParser.Parse(typeDef.Directives);
        var lookups = LookupDirectiveParser.Parse(typeDef.Directives);
        var temp = new SourceInterfaceType[types.Length];

        for (var i = 0; i < types.Length; i++)
        {
            var type = types[i];
            temp[i] = new SourceInterfaceType(
                typeDef.Name.Value,
                type.SchemaName,
                GetLookupBySchema(lookups, type.SchemaName));
        }

        return new SourceInterfaceTypeCollection(temp);
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

                var fieldBuilder = ImmutableArray.CreateBuilder<FieldPath>();

                foreach (var field in lookup.Map)
                {
                    fieldBuilder.Add(FieldPath.Parse(field));
                }

                var fields = fieldBuilder.ToImmutable();
                var selectionSet = fields.ToSelectionSetNode();
                
                lookups.Add(
                    new Lookup(
                        lookup.SchemaName,
                        lookup.Field.Name.Value,
                        arguments.ToImmutable(),
                        fieldBuilder.ToImmutable(),
                        selectionSet));
            }
        }

        return lookups.ToImmutable();
    }
}
