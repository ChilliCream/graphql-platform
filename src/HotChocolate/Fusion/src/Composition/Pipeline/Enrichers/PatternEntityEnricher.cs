using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using HotChocolate.Language;
using HotChocolate.Skimmed;
using static System.StringComparison;

namespace HotChocolate.Fusion.Composition.Pipeline;

internal sealed partial class PatternEntityEnricher : IEntityEnricher
{
    [GeneratedRegex("^(.*?[a-z0-9])(By)([A-Z].*)$")]
    private static partial Regex CreateRegex();

    public ValueTask EnrichAsync(
        CompositionContext context,
        EntityGroup entity,
        CancellationToken cancellationToken = default)
    {
        var regex = CreateRegex();
        
        foreach (var (type, schema) in entity.Parts)
        {
            if (schema.QueryType is null)
            {
                continue;
            }
            
            foreach (var entityResolver in schema.QueryType.Fields)
            {
                var originalTypeName = type.GetOriginalName();
                if (entityResolver.Name.AsSpan().StartsWith(originalTypeName, OrdinalIgnoreCase))
                {
                    var splits = regex.Split(entityResolver.Name);
                    if (splits.Length == 5)
                    {
                        var typeName = splits[1];
                        var fieldName = splits[3];
                        
                        if (typeName.Equals(originalTypeName, OrdinalIgnoreCase))
                        {
                            var field = type.Fields.FirstOrDefault(f => f.Name.Equals(fieldName, OrdinalIgnoreCase));
                            if (field is not null)
                            {
                                TryRegisterEntityResolver(entity, type, entityResolver, field, schema);
                            }
                        }
                        else if (typeName.Length - 1 == originalTypeName.Length && 
                            typeName.AsSpan()[typeName.Length - 1] == 's')
                        {
                            
                        }
                    }
                }
            }
        }
        return default;
    }
    
    private static void TryRegisterEntityResolver(
        EntityGroup entity,
        ObjectType entityType,
        OutputField entityResolverField,
        OutputField keyField,
        Schema schema)
    {
        // Check if the query field type matches the entity type
        // and if it has any arguments that contain the @is directive
        if (entityResolverField.Arguments.Count == 1 && 
            (entityResolverField.Type == entityType ||
            (entityResolverField.Type.Kind is TypeKind.NonNull &&
                entityResolverField.Type.InnerType() == entityType)))
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
            var resolver = new EntityResolver(
                EntityResolverKind.Single,
                selectionSet,
                entityType.Name,
                schema.Name);
            
            var keyFieldNode = new FieldNode(
                null,
                new NameNode(keyField.Name),
                null,
                null,
                Array.Empty<DirectiveNode>(),
                Array.Empty<ArgumentNode>(),
                null);

            var keyFieldDirective = new IsDirective(keyFieldNode);
            var arg = entityResolverField.Arguments.First();
            var var = entityType.CreateVariableName(keyFieldDirective);
            arguments.Add(new ArgumentNode(arg.Name, new VariableNode(var)));
            resolver.Variables.Add(var, arg.CreateVariableField(keyFieldDirective, var));

            // Add the new EntityResolver to the entity metadata
            entity.Metadata.EntityResolvers.Add(resolver);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsListOf(IType type, IType entityType)
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
    private static bool IsListOfScalar(IType type)
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