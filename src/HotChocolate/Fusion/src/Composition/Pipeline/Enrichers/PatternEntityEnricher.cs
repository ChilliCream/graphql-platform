using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using HotChocolate.Language;
using HotChocolate.Skimmed;
using HotChocolate.Types;
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
                        var isList = entityResolver.Type.IsListType();

                        if (!isList && typeName.Equals(originalTypeName, OrdinalIgnoreCase))
                        {
                            var field = type.Fields.FirstOrDefault(f => f.Name.Equals(fieldName, OrdinalIgnoreCase));
                            if (field is not null)
                            {
                                TryRegisterEntityResolver(entity, type, entityResolver, field, schema);
                            }
                        }
                        else if (isList && typeName.Equals(originalTypeName, OrdinalIgnoreCase) ||
                            (typeName.Length - 1 == originalTypeName.Length &&
                                typeName.AsSpan()[typeName.Length - 1] == 's'))
                        {
                            var field = type.Fields.FirstOrDefault(f => f.Name.Equals(fieldName, OrdinalIgnoreCase));

                            if (field is null)
                            {
                                var fieldPlural = fieldName[..^1];
                                field = type.Fields.FirstOrDefault(f => f.Name.Equals(fieldPlural, OrdinalIgnoreCase));
                            }

                            if (field is not null)
                            {
                                TryRegisterBatchEntityResolver(entity, type, entityResolver, field, schema);
                            }
                        }
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
        OutputFieldDefinition keyField,
        SchemaDefinition schema)
    {
        if (!TryResolveKeyArgument(entityResolverField, keyField, out var keyArg))
        {
            return;
        }

        if (entityResolverField.Type == entityType ||
            (entityResolverField.Type.Kind is TypeKind.NonNull &&
                entityResolverField.Type.InnerType() == entityType))
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

            var keyFieldNode = new FieldNode(
                null,
                new NameNode(keyField.Name),
                null,
                Array.Empty<DirectiveNode>(),
                Array.Empty<ArgumentNode>(),
                null);

            var keyFieldDirective = new IsDirective(keyFieldNode);
            var var = entityType.CreateVariableName(keyFieldDirective);
            arguments.Add(new ArgumentNode(keyArg.Name, new VariableNode(var)));
            resolver.Variables.Add(var, keyArg.CreateVariableField(keyFieldDirective, var));

            // Add the new EntityResolver to the entity metadata
            entity.Metadata.EntityResolvers.TryAdd(resolver);
        }
    }

    private static bool TryResolveKeyArgument(
        OutputFieldDefinition entityResolverField,
        OutputFieldDefinition keyField,
        [NotNullWhen(true)] out InputFieldDefinition? keyArgument)
    {
        if (entityResolverField.Arguments.TryGetField(keyField.Name, out keyArgument))
        {
            return !keyArgument.ContainsIsDirective() &&
                keyArgument.Type.Equals(keyField.Type, TypeComparison.Structural);
        }

        if (entityResolverField.Arguments.Count == 1)
        {
            keyArgument = entityResolverField.Arguments.First();
        }
        else
        {
            foreach (var argument in entityResolverField.Arguments)
            {
                if (keyArgument is null)
                {
                    keyArgument = argument;
                    continue;
                }

                if (argument.Type.Kind is not TypeKind.NonNull)
                {
                    continue;
                }

                if (argument.DefaultValue is null)
                {
                    keyArgument = null;
                    return false;
                }
            }
        }

        return (keyArgument?.Type.Equals(keyField.Type, TypeComparison.Structural) ?? false) &&
            !keyArgument.ContainsIsDirective();
    }

    private static void TryRegisterBatchEntityResolver(
        EntityGroup entity,
        ObjectTypeDefinition entityType,
        OutputFieldDefinition entityResolverField,
        OutputFieldDefinition keyField,
        SchemaDefinition schema)
    {
        if (!TryResolveBatchKeyArgument(entityResolverField, keyField, out var keyArg))
        {
            return;
        }

        var returnType = entityResolverField.Type;

        if (returnType.Kind is TypeKind.NonNull)
        {
            returnType = returnType.InnerType();
        }

        if(returnType.Kind != TypeKind.List)
        {
            return;
        }

        returnType = returnType.InnerType();

        if (returnType == entityType ||
            (returnType.Kind is TypeKind.NonNull &&
                returnType.InnerType() == entityType))
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
                entityType.Name,
                schema.Name);

            var keyFieldNode = new FieldNode(
                null,
                new NameNode(keyField.Name),
                null,
                Array.Empty<DirectiveNode>(),
                Array.Empty<ArgumentNode>(),
                null);

            var keyFieldDirective = new IsDirective(keyFieldNode);
            var var = entityType.CreateVariableName(keyFieldDirective);
            arguments.Add(new ArgumentNode(keyArg.Name, new VariableNode(var)));
            resolver.Variables.Add(var, keyArg.CreateVariableField(keyFieldDirective, var));

            // Add the new EntityResolver to the entity metadata
            entity.Metadata.EntityResolvers.TryAdd(resolver);
        }
    }

    private static bool TryResolveBatchKeyArgument(
        OutputFieldDefinition entityResolverField,
        OutputFieldDefinition keyField,
        [NotNullWhen(true)] out InputFieldDefinition? keyArgument)
    {
        if (entityResolverField.Arguments.TryGetField(keyField.Name, out keyArgument))
        {
            if (keyArgument.Type.IsListType() && !keyArgument.ContainsIsDirective())
            {
                var argumentType = keyArgument.Type;
                if (argumentType.Kind is TypeKind.NonNull)
                {
                    argumentType = argumentType.InnerType();
                }

                return argumentType.InnerType().Equals(keyField.Type, TypeComparison.Structural);
            }

            keyArgument = null;
            return false;
        }

        if (entityResolverField.Arguments.Count == 1)
        {
            keyArgument = entityResolverField.Arguments.First();

            if (keyArgument.Type.IsListType() && !keyArgument.ContainsIsDirective())
            {
                var argumentType = keyArgument.Type;
                if (argumentType.Kind is TypeKind.NonNull)
                {
                    argumentType = argumentType.InnerType();
                }

                return argumentType.InnerType().Equals(keyField.Type, TypeComparison.Structural);
            }

            keyArgument = null;
            return false;
        }

        foreach (var argument in entityResolverField.Arguments)
        {
            if (keyArgument is null)
            {
                keyArgument = argument;
                continue;
            }

            if (argument.Type.Kind is not TypeKind.NonNull)
            {
                continue;
            }

            if (argument.DefaultValue is null)
            {
                keyArgument = null;
                return false;
            }
        }

        if (keyArgument?.Type.IsListType() is true && !keyArgument.ContainsIsDirective())
        {
            var argumentType = keyArgument.Type;
            if (argumentType.Kind is TypeKind.NonNull)
            {
                argumentType = argumentType.InnerType();
            }

            return argumentType.InnerType().Equals(keyField.Type, TypeComparison.Structural);
        }

        keyArgument = null;
        return false;
    }
}
