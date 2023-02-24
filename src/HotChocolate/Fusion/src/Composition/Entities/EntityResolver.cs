using HotChocolate.Language;

namespace HotChocolate.Fusion.Composition;

public sealed class EntityResolver
{
    public EntityResolver(FieldNode field, string entityName, string schemaName)
    {
        Field = field;
        EntityName = entityName;
        SchemaName = schemaName;
    }

    public FieldNode Field { get; }

    public string EntityName { get; }

    public string SchemaName { get; }

    public Dictionary<string, VariableDefinition> Variables { get; } = new();

    public override string ToString()
    {
        var definitions = new List<IDefinitionNode>();

        definitions.Add(
            new OperationDefinitionNode(
                null,
                null,
                OperationType.Query,
                Variables.Select(t => t.Value.Definition).ToList(),
                new[] { new DirectiveNode("schema", new ArgumentNode("name", SchemaName)) },
                new SelectionSetNode(new[] { Field })));

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
