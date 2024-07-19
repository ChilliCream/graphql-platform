using System.Runtime.CompilerServices;
using HotChocolate.Language;
using HotChocolate.Skimmed;
using HotChocolate.Types;

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
            if (schema.QueryType is null)
            {
                continue;
            }

            // Loop through each query field
            foreach (var entityResolverField in schema.QueryType.Fields)
            {
                TryRegisterEntityResolver(entity, type, entityResolverField, schema);

                // Check if the query field can be used to infer a batch by key resolver.
                if (IsListOf(entityResolverField.Type, type) &&
                    entityResolverField.Arguments.Count == 1)
                {
                    var argument = entityResolverField.Arguments.First();

                    if (argument.ContainsIsDirective() && IsListOfScalar(argument.Type))
                    {
                        var arguments = new List<ArgumentNode>();

                        // Create a new FieldNode for the entity resolver
                        var selection = new FieldNode(
                            null,
                            new NameNode(entityResolverField.GetOriginalName()),
                            null,
                            Array.Empty<DirectiveNode>(),
                            arguments,
                            null);

                        // Create a new SelectionSetNode for the entity resolver
                        var selectionSet = new SelectionSetNode(new[] { selection, });

                        // Create a new EntityResolver for the entity
                        var resolver = new EntityResolver(
                            EntityResolverKind.Batch,
                            selectionSet,
                            type.Name,
                            schema.Name);

                        // Loop through each argument and create a new ArgumentNode
                        // and VariableNode for the @ref directive argument
                        foreach (var arg in entityResolverField.Arguments)
                        {
                            var directive = arg.GetIsDirective();
                            var var = type.CreateVariableName(directive);
                            arguments.Add(new ArgumentNode(arg.Name, new VariableNode(var)));
                            resolver.Variables.Add(
                                var,
                                arg.CreateVariableField(directive, var));
                        }

                        // Add the new EntityResolver to the entity metadata
                        entity.Metadata.EntityResolvers.TryAdd(resolver);
                    }
                }
            }
        }

        return default;
    }

    private static void TryRegisterEntityResolver(
        EntityGroup entity,
        ObjectTypeDefinition entityType,
        OutputFieldDefinition entityResolverField,
        SchemaDefinition schema)
    {
        // Check if the query field type matches the entity type
        // and if it has any arguments that contain the @is directive
        if ((entityResolverField.Type == entityType ||
                (entityResolverField.Type.Kind is TypeKind.NonNull &&
                    entityResolverField.Type.InnerType() == entityType)) &&
            entityResolverField.Arguments.Count > 0 &&
            entityResolverField.Arguments.All(t => t.ContainsIsDirective()))
        {
            var arguments = new List<ArgumentNode>();

            // Create a new FieldNode for the entity resolver
            var selection = new FieldNode(
                null,
                new NameNode(entityResolverField.GetOriginalName()),
                null,
                Array.Empty<DirectiveNode>(),
                arguments,
                null);

            // Create a new SelectionSetNode for the entity resolver
            var selectionSet = new SelectionSetNode(new[] { selection, });

            // Create a new EntityResolver for the entity
            var resolver = new EntityResolver(
                EntityResolverKind.Single,
                selectionSet,
                entityType.Name,
                schema.Name);

            // Loop through each argument and create a new ArgumentNode
            // and VariableNode for the @is directive argument
            foreach (var arg in entityResolverField.Arguments)
            {
                var directive = arg.GetIsDirective();
                var var = entityType.CreateVariableName(directive);
                arguments.Add(new ArgumentNode(arg.Name, new VariableNode(var)));
                resolver.Variables.Add(var, arg.CreateVariableField(directive, var));
            }

            // Add the new EntityResolver to the entity metadata
            entity.Metadata.EntityResolvers.TryAdd(resolver);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsListOf(ITypeDefinition type, ITypeDefinition entityType)
    {
        if (type.Kind == TypeKind.NonNull)
        {
            type = type.InnerType();
        }

        if (type.Kind != TypeKind.List)
        {
            return false;
        }

        type = type.InnerType();

        if (type.Kind == TypeKind.NonNull)
        {
            type = type.InnerType();
        }

        return ReferenceEquals(type, entityType);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsListOfScalar(ITypeDefinition type)
    {
        if (type.Kind == TypeKind.NonNull)
        {
            type = type.InnerType();
        }

        if (type.Kind != TypeKind.List)
        {
            return false;
        }

        type = type.InnerType();

        if (type.Kind == TypeKind.NonNull)
        {
            type = type.InnerType();
        }

        return type.Kind == TypeKind.Scalar;
    }
}
