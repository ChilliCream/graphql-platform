using HotChocolate.Language;

namespace HotChocolate.Fusion.Composition;

/// <summary>
/// Represents an entity resolver for retrieving data for the entity from a subgraph.
/// </summary>
internal sealed class EntityResolver
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EntityResolver"/> class.
    /// </summary>
    /// <param name="kind">
    /// The kind of entity resolver. This is used to determine how to resolve the entity.
    /// </param>
    /// <param name="selectionSet">
    /// The selection set for the entity.
    /// </param>
    /// <param name="entityName">
    /// The name of the entity being resolved.
    /// </param>
    /// <param name="subgraphName">
    /// The name of the schema that contains the entity.
    /// </param>
    public EntityResolver(
        EntityResolverKind kind,
        SelectionSetNode selectionSet,
        string entityName,
        string subgraphName)
    {
        Kind = kind;
        SelectionSet = selectionSet ?? throw new ArgumentNullException(nameof(selectionSet));
        EntityName = entityName ?? throw new ArgumentNullException(nameof(entityName));
        SubgraphName = subgraphName ?? throw new ArgumentNullException(nameof(subgraphName));
    }

    /// <summary>
    /// Gets the type of this resolver.
    /// </summary>
    public EntityResolverKind Kind { get; }

    /// <summary>
    /// Gets the selection set that specifies how to retrieve data for the entity.
    /// </summary>
    public SelectionSetNode SelectionSet { get; }

    /// <summary>
    /// Gets the name of the entity being resolved.
    /// </summary>
    public string EntityName { get; }

    /// <summary>
    /// Gets the name of the subgraph that contains data for this entity.
    /// </summary>
    public string SubgraphName { get; }

    /// <summary>
    /// Gets the variables used in the resolver.
    /// </summary>
    public Dictionary<string, VariableDefinition> Variables { get; } = new();

    /// <summary>
    /// Returns a string representation of the entity resolver.
    /// </summary>
    /// <returns>A string representation of the entity resolver.</returns>
    public override string ToString()
    {
        var definitions = new List<IDefinitionNode>
        {
            new OperationDefinitionNode(
                null,
                null,
                OperationType.Query,
                Variables.Select(t => t.Value.Definition).ToList(),
                new[] { new DirectiveNode("schema", new ArgumentNode("name", SubgraphName)), },
                SelectionSet),
        };

        if (Variables.Count > 0)
        {
            definitions.Add(
                new FragmentDefinitionNode(
                    null,
                    new NameNode("Requirements"),
                    Array.Empty<VariableDefinitionNode>(),
                    new NamedTypeNode(EntityName),
                    Array.Empty<DirectiveNode>(),
                    new SelectionSetNode(Variables.Select(static t => Format(t)).ToList())));
        }

        return new DocumentNode(definitions).ToString(true);

        static FieldNode Format(KeyValuePair<string, VariableDefinition> item)
        {
            var directives = item.Value.Field.Directives.ToList();
            directives.Add(new DirectiveNode("variable", new ArgumentNode("name", item.Key)));
            return item.Value.Field.WithDirectives(directives);
        }
    }
}
