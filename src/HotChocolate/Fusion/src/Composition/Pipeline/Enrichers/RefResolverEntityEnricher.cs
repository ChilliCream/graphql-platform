using HotChocolate.Language;
using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.Composition.Pipeline;

/// <summary>
/// A pipeline enricher that processes entity groups and adds entity resolvers to
/// metadata for all arguments that contain the @ref directive.
/// </summary>
internal sealed class RefResolverEntityEnricher : IEntityEnricher
{
    /// <inheritdoc />
    public ValueTask EnrichAsync(
        CompositionContext context,
        EntityGroup entity,
        CancellationToken cancellationToken = default)
    {
        foreach (var (type, schema) in entity.Parts)
        {
            // Check if the schema has a query type
            if (schema.QueryType is not null)
            {
                // Loop through each query field
                foreach (var entityResolverField in schema.QueryType.Fields)
                {
                    // Check if the query field type matches the entity type
                    // and if it has any arguments that contain the @ref directive
                    if ((entityResolverField.Type == type ||
                            entityResolverField.Type.Kind is TypeKind.NonNull &&
                            entityResolverField.Type.InnerType() == type) &&
                        entityResolverField.Arguments.All(t => t.ContainsIsDirective()))
                    {
                        var arguments = new List<ArgumentNode>();

                        // Create a new FieldNode for the entity resolver
                        var selection = new FieldNode(
                            null,
                            new NameNode(entityResolverField.GetOriginalName()),
                            null,
                            null,
                            Array.Empty<DirectiveNode>(),
                            arguments,
                            null);

                        // Create a new SelectionSetNode for the entity resolver
                        var selectionSet = new SelectionSetNode(new[] { selection });

                        // Create a new EntityResolver for the entity
                        var resolver = new EntityResolver(selectionSet, type.Name, schema.Name);

                        // Loop through each argument and create a new ArgumentNode
                        // and VariableNode for the @ref directive argument
                        foreach (var arg in entityResolverField.Arguments)
                        {
                            var directive = arg.GetIsDirective();
                            var var = type.CreateVariableName(directive);
                            arguments.Add(new ArgumentNode(arg.Name, new VariableNode(var)));
                            resolver.Variables.Add(var, arg.CreateVariableField(directive, var));
                        }

                        // Add the new EntityResolver to the entity metadata
                        entity.Metadata.EntityResolvers.Add(resolver);
                    }
                }
            }
        }

        return default;
    }
}
