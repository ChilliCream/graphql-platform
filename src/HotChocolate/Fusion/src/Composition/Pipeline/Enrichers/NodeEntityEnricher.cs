using HotChocolate.Language;
using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.Composition.Pipeline;

internal class NodeEntityEnricher : IEntityEnricher
{
    private static readonly FieldNode _idField =
        new FieldNode(
            null,
            new NameNode("id"),
            null,
            Array.Empty<DirectiveNode>(),
            Array.Empty<ArgumentNode>(),
            null);

    private static readonly VariableDefinitionNode _idVariable =
        new VariableDefinitionNode(
            null,
            new VariableNode("var"),
            new NonNullTypeNode(new NamedTypeNode("ID")),
            null,
            Array.Empty<DirectiveNode>());

    private static readonly VariableDefinitionNode _idsVariable =
        new VariableDefinitionNode(
            null,
            new VariableNode("var"),
            new NonNullTypeNode(new ListTypeNode(new NonNullTypeNode(new NamedTypeNode("ID")))),
            null,
            Array.Empty<DirectiveNode>());

    public ValueTask EnrichAsync(
        CompositionContext context,
        EntityGroup entity,
        CancellationToken cancellationToken = default)
    {
        foreach (var (type, schema) in entity.Parts)
        {
            if (schema.QueryType is not null &&
                schema.QueryType.Fields.ContainsName("node") &&
                schema.Types.TryGetType<InterfaceTypeDefinition>("Node", out var nodeType) &&
                type.Implements.Contains(nodeType))
            {
                ResolveWithNode(entity, schema, type, entity.Name);

                if (schema.QueryType.Fields.ContainsName("nodes"))
                {
                    ResolveWithNodes(entity, schema, type, entity.Name);
                }
            }
        }
        return default;
    }

    private static void ResolveWithNode(
        EntityGroup entity,
        SchemaDefinition sourceSchema,
        ObjectTypeDefinition sourceType,
        string targetName)
    {
        var arguments = new List<ArgumentNode>();

        var spread = new FragmentSpreadNode(
            null,
            new NameNode(targetName),
            Array.Empty<DirectiveNode>());

        var inlineFragment = new InlineFragmentNode(
            null,
            new NamedTypeNode(sourceType.Name),
            Array.Empty<DirectiveNode>(),
            new SelectionSetNode(new[] { spread, }));

        // Create a new FieldNode for the entity resolver
        var selection = new FieldNode(
            null,
            new NameNode("node"),
            null,
            Array.Empty<DirectiveNode>(),
            arguments,
            new SelectionSetNode(new[] { inlineFragment, }));

        // Create a new SelectionSetNode for the entity resolver
        var selectionSet = new SelectionSetNode(new[] { selection, });

        // Create a new EntityResolver for the entity
        var resolver = new EntityResolver(
            EntityResolverKind.Single,
            selectionSet,
            sourceType.Name,
            sourceSchema.Name);

        var var = sourceType.CreateVariableName(new SchemaCoordinate(targetName, "id"));
        var varNode = new VariableNode(var);
        arguments.Add(new ArgumentNode("id", varNode));

        resolver.Variables.Add(
            var,
            new VariableDefinition(
                var,
                _idField,
                _idVariable.WithVariable(varNode)));

        // Add the new EntityResolver to the entity metadata
        entity.Metadata.EntityResolvers.TryAdd(resolver);
    }

    private static void ResolveWithNodes(
        EntityGroup entity,
        SchemaDefinition sourceSchema,
        ObjectTypeDefinition sourceType,
        string targetName)
    {
        var arguments = new List<ArgumentNode>();

        var spread = new FragmentSpreadNode(
            null,
            new NameNode(targetName),
            Array.Empty<DirectiveNode>());

        var inlineFragment = new InlineFragmentNode(
            null,
            new NamedTypeNode(sourceType.Name),
            Array.Empty<DirectiveNode>(),
            new SelectionSetNode(new[] { spread, }));

        // Create a new FieldNode for the entity resolver
        var selection = new FieldNode(
            null,
            new NameNode("nodes"),
            null,
            Array.Empty<DirectiveNode>(),
            arguments,
            new SelectionSetNode(new[] { inlineFragment, }));

        // Create a new SelectionSetNode for the entity resolver
        var selectionSet = new SelectionSetNode(new[] { selection, });

        // Create a new EntityResolver for the entity
        var resolver = new EntityResolver(
            EntityResolverKind.Batch,
            selectionSet,
            sourceType.Name,
            sourceSchema.Name);

        var var = sourceType.CreateVariableName(new SchemaCoordinate(targetName, "id"));
        var varNode = new VariableNode(var);
        arguments.Add(new ArgumentNode("ids", varNode));

        resolver.Variables.Add(
            var,
            new VariableDefinition(
                var,
                _idField,
                _idsVariable.WithVariable(varNode)));

        // Add the new EntityResolver to the entity metadata
        entity.Metadata.EntityResolvers.TryAdd(resolver);
    }
}
