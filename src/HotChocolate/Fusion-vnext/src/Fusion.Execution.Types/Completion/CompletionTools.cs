using System.Collections.Immutable;
using System.Runtime.InteropServices;
using HotChocolate.Fusion.Language;
using HotChocolate.Fusion.Types.Collections;
using HotChocolate.Fusion.Types.Directives;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Types.Completion;

internal static class CompletionTools
{
    public static FusionDirectiveCollection CreateDirectiveCollection(
        IReadOnlyList<DirectiveNode> directives,
        CompositeSchemaBuilderContext context)
    {
        directives = DirectiveTools.GetUserDirectives(directives);

        if (directives.Count == 0)
        {
            return FusionDirectiveCollection.Empty;
        }

        var temp = new FusionDirective[directives.Count];

        for (var i = 0; i < directives.Count; i++)
        {
            var directive = directives[i];
            var directiveType = context.GetDirectiveType(directive.Name.Value);
            var arguments = CreateArgumentAssignments(directive.Arguments);
            temp[i] = new FusionDirective(
                directiveType,
                ImmutableCollectionsMarshal.AsImmutableArray(arguments));
        }

        return new FusionDirectiveCollection(temp);
    }

    private static ArgumentAssignment[] CreateArgumentAssignments(
        IReadOnlyList<ArgumentNode> arguments)
    {
        if (arguments.Count == 0)
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

    public static FusionInterfaceTypeDefinitionCollection CreateInterfaceTypeCollection(
        IReadOnlyList<NamedTypeNode> interfaceTypes,
        CompositeSchemaBuilderContext context)
    {
        if (interfaceTypes.Count == 0)
        {
            return FusionInterfaceTypeDefinitionCollection.Empty;
        }

        var temp = new FusionInterfaceTypeDefinition[interfaceTypes.Count];

        for (var i = 0; i < interfaceTypes.Count; i++)
        {
            temp[i] = (FusionInterfaceTypeDefinition)context.GetType(interfaceTypes[i]);
        }

        return new FusionInterfaceTypeDefinitionCollection(temp);
    }

    public static FusionObjectTypeDefinitionCollection CreateObjectTypeCollection(
        IReadOnlyList<NamedTypeNode> types,
        CompositeSchemaBuilderContext context)
    {
        var temp = new FusionObjectTypeDefinition[types.Count];

        for (var i = 0; i < types.Count; i++)
        {
            temp[i] = (FusionObjectTypeDefinition)context.GetType(types[i]);
        }

        return new FusionObjectTypeDefinitionCollection(temp);
    }

    public static SourceObjectTypeCollection CreateSourceObjectTypeCollection(
        ObjectTypeDefinitionNode typeDef,
        CompositeSchemaBuilderContext context)
    {
        var types = TypeDirectiveParser.Parse(typeDef.Directives);
        var lookupDirectives = LookupDirectiveParser.Parse(typeDef.Directives);
        var temp = new SourceObjectType[types.Length];

        for (var i = 0; i < types.Length; i++)
        {
            var type = types[i];
            var lookups = GetLookupBySchema(lookupDirectives, type.SchemaName, typeDef.Name.Value);
            context.RegisterForCompletionRange(lookups);

            temp[i] = new SourceObjectType(
                typeDef.Name.Value,
                type.SchemaName,
                lookups);
        }

        return new SourceObjectTypeCollection(temp);
    }

    public static SourceInterfaceTypeCollection CreateSourceInterfaceTypeCollection(
        InterfaceTypeDefinitionNode typeDef,
        CompositeSchemaBuilderContext context)
    {
        var types = TypeDirectiveParser.Parse(typeDef.Directives);
        var lookupDirectives = LookupDirectiveParser.Parse(typeDef.Directives);
        var temp = new SourceInterfaceType[types.Length];

        for (var i = 0; i < types.Length; i++)
        {
            var type = types[i];
            var lookups = GetLookupBySchema(lookupDirectives, type.SchemaName, typeDef.Name.Value);
            context.RegisterForCompletionRange(lookups);

            temp[i] = new SourceInterfaceType(
                typeDef.Name.Value,
                type.SchemaName,
                lookups);
        }

        return new SourceInterfaceTypeCollection(temp);
    }

    private static ImmutableArray<Lookup> GetLookupBySchema(
        ImmutableArray<LookupDirective> allLookups,
        string schemaName,
        string declaringTypeName)
    {
        var lookups = ImmutableArray.CreateBuilder<Lookup>();

        foreach (var lookup in allLookups)
        {
            if (lookup.Schema.Equals(schemaName, StringComparison.Ordinal))
            {
                var arguments = ImmutableArray.CreateBuilder<LookupArgument>(lookup.Field.Arguments.Count);

                foreach (var argument in lookup.Field.Arguments)
                {
                    arguments.Add(new LookupArgument(argument.Name.Value, argument.Type));
                }

                var fieldsBuilder = ImmutableArray.CreateBuilder<IValueSelectionNode>();

                foreach (var field in lookup.Map)
                {
                    var parser = new FieldSelectionMapParser(field);
                    fieldsBuilder.Add(parser.Parse());
                }

                var fields = fieldsBuilder.ToImmutable();

                lookups.Add(
                    new Lookup(
                        lookup.Schema,
                        declaringTypeName,
                        lookup.Field.Name.Value,
                        arguments.ToImmutable(),
                        fields));
            }
        }

        return lookups.ToImmutable();
    }
}
