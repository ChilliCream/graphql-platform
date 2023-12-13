using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using HotChocolate.Language;
using HotChocolate.Skimmed;
using HotChocolate.Utilities;
using static HotChocolate.Fusion.Composition.LogEntryHelper;
using static HotChocolate.Fusion.Composition.Pipeline.MergeHelper;

namespace HotChocolate.Fusion.Composition.Pipeline;

/// <summary>
/// A type handler that is responsible for merging input object types into a single distributed
/// input object type on the fusion graph.
/// </summary>
internal sealed partial class QueryTypeMergeHandler : ITypeMergeHandler
{
    [GeneratedRegex("^(.*?[a-z0-9])(By)([A-Z].*)$")]
    private static partial Regex CreateRegex();

    /// <inheritdoc />
    public TypeKind Kind => TypeKind.Object;

    /// <inheritdoc />
    public MergeStatus Merge(CompositionContext context, TypeGroup typeGroup)
    {
        // If any type in the group is not an input object type, skip merging
        if (typeGroup.Parts.Any(t => t.Type.Kind is not TypeKind.Object))
        {
            context.Log.Write(DifferentTypeKindsCannotBeMerged(typeGroup));
            return MergeStatus.Skipped;
        }

        // Get the target input object type from the fusion graph
        var target = GetOrCreateType<ObjectType>(context.FusionGraph, typeGroup.Name);

        // Merge each part of the input object type into the target input object type
        foreach (var part in typeGroup.Parts)
        {
            var source = (ObjectType)part.Type;
            MergeType(context, source, part.Schema, target, context.FusionGraph, CreateRegex());
        }

        return MergeStatus.Completed;
    }

    private static void MergeType(
        CompositionContext context,
        ObjectType source,
        Schema sourceSchema,
        ObjectType target,
        Schema targetSchema,
        Regex byFieldPattern)
    {
        // If the target input object type doesn't have a description, use the source input
        // object type's description
        target.MergeDescriptionWith(source);

        // Add all of the interfaces that the source type implements to the target type.
        foreach (var interfaceType in source.Implements)
        {
            if (!target.Implements.Any(t => t.Name.EqualsOrdinal(interfaceType.Name)))
            {
                target.Implements.Add(GetOrCreateType<InterfaceType>(context.FusionGraph, interfaceType.Name));
            }
        }

        // Merge each field of the input object type
        foreach (var sourceField in source.Fields)
        {
            if (target.Fields.TryGetField(sourceField.Name, out var targetField))
            {
                // If the target input object type has a field with the same name as the source
                // field, merge the source field into the target field
                context.MergeField(sourceField, targetField, target.Name);
            }
            else
            {
                // If the target input object type doesn't have a field with the same name as
                // the source field, create a new target field with the source field's
                // properties
                targetField = context.CreateField(sourceField, targetSchema);
                target.Fields.Add(targetField);
            }

            AddRootResolver(context, sourceSchema, sourceField, targetField);

            // if we find a query field that returns an object type,
            // we need to check if the query field can be used as an entity resolver.
            if (targetField.Type.NamedType().Kind is TypeKind.Object)
            {
                if (TryExtractAnnotationBasedEntityResolver(
                    context, 
                    sourceSchema, 
                    sourceField, 
                    targetField))
                {
                    continue;
                }

                if (TryExtractNameBasedEntityResolver(
                    context,
                    sourceField,
                    targetField,
                    (ObjectType)sourceField.Type.NamedType(),
                    sourceSchema,
                    byFieldPattern))
                {
                    continue;
                }
                
                
            }
        }
    }

    private static void AddRootResolver(
        CompositionContext context,
        Schema sourceSchema,
        OutputField sourceField,
        OutputField targetField)
    {
        var arguments = new List<ArgumentNode>();
        var variables = new List<VariableDefinitionNode>();

        var selection = new FieldNode(
            null,
            new NameNode(targetField.GetOriginalName()),
            null,
            null,
            Array.Empty<DirectiveNode>(),
            arguments,
            null);

        foreach (var arg in sourceField.Arguments)
        {
            var variableType = arg.Type.ToTypeNode(arg.Type.NamedType().GetOriginalName());
            var variable = new VariableNode(arg.Name);
            arguments.Add(new ArgumentNode(arg.Name, variable));
            variables.Add(new VariableDefinitionNode(variable, variableType, null, Array.Empty<DirectiveNode>()));
        }

        var operation = new OperationDefinitionNode(
            OperationType.Query,
            variables,
            Array.Empty<DirectiveNode>(),
            new SelectionSetNode(selection));

        var resolver = new ResolverDirective(operation, ResolverKind.Fetch, sourceSchema.Name);
        targetField.Directives.Add(resolver.ToDirective(context.FusionTypes));
        SourceDirective.RemoveFrom(targetField, context.FusionTypes, sourceSchema.Name);
    }

    private static bool TryExtractAnnotationBasedEntityResolver(
        CompositionContext context,
        Schema sourceSchema,
        OutputField sourceField,
        OutputField targetField)
    {
        if (sourceField.Arguments.Count == 0)
        {
            return false;
        }

        List<EntitySourceArgument>? arguments = null;
        var kind = ResolverKind.Fetch;

        foreach (var argument in sourceField.Arguments)
        {
            if (kind is ResolverKind.Fetch && argument.Type.IsListType())
            {
                if (!sourceField.Type.IsListType())
                {
                    // TODO : ERROR
                    context.Log.Write(
                        new LogEntry(
                            "The annotated field looks like a batch field but has only a single return value.",
                            severity: LogSeverity.Warning));
                    return false;
                }

                kind = ResolverKind.Batch;
            }

            if (!IsDirective.ExistsIn(argument, context.FusionTypes))
            {
                return false;
            }

            var directive = IsDirective.TryGetFrom(argument, context.FusionTypes);

            if (directive is null)
            {
                // TODO : ERROR
                context.Log.Write(
                    new LogEntry(
                        "The is directive must have a value for coordinate or field.",
                        severity: LogSeverity.Error));
                return false;
            }

            (arguments ??= new()).Add(new EntitySourceArgument(argument, directive));
        }

        context.EntityResolverInfos.Add(
            new EntityResolverInfo(
                targetField.Type.NamedType().Name,
                sourceField.Type.NamedType().GetOriginalName(),
                kind,
                targetField,
                new EntitySourceField(sourceSchema, sourceField),
                arguments!));
        return true;
    }

    private static bool TryExtractNameBasedEntityResolver(
        CompositionContext context,
        OutputField entityResolverField,
        OutputField entityResolverTargetField,
        ObjectType entityType,
        Schema schema,
        Regex byFieldPattern)
    {
        var originalTypeName = entityType.GetOriginalName();
        var originalFieldName = entityResolverField.GetOriginalName();

        if (!PossibleTypeNameMatch(originalTypeName, originalFieldName))
        {
            return false;
        }
        
        var splits = byFieldPattern.Split(originalFieldName);

        if (splits.Length == 5)
        {
            var typeName = splits[1];
            var fieldName = splits[3];
            var isList = entityResolverField.Type.IsListType();

            if (!isList && typeName.Equals(originalTypeName, StringComparison.OrdinalIgnoreCase))
            {
                if (entityType.Fields.TryGetField(fieldName, StringComparison.OrdinalIgnoreCase, out var keyField))
                {
                    TryRegisterEntityResolver(
                        context,
                        entityResolverField,
                        entityResolverTargetField,
                        entityType,
                        keyField,
                        schema);
                }
            }
            else if (isList && DoesTypeNameMatch(originalTypeName, typeName))
            {
                if (!entityType.Fields.TryGetField(fieldName, StringComparison.OrdinalIgnoreCase, out var keyField))
                {
                    var fieldPlural = fieldName[..^1];
                    entityType.Fields.TryGetField(fieldPlural, StringComparison.OrdinalIgnoreCase, out keyField);
                }

                if (keyField is not null)
                {
                    TryRegisterBatchEntityResolver(
                        context,
                        entityResolverField,
                        entityResolverTargetField,
                        entityType,
                        keyField,
                        schema);
                }
            }
        }
    }
    
    private static bool TryExtractEntityResolverFromNodeField(
        CompositionContext context,
        Schema sourceSchema,
        OutputField sourceField,
        OutputField targetField)
    {
        
        return true;
    }
    
    private static bool PossibleTypeNameMatch(ReadOnlySpan<char> typeName, ReadOnlySpan<char> fieldName)
    {
        if (fieldName.StartsWith(typeName, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var end = typeName.Length;
        if (typeName.EndsWith("y", StringComparison.Ordinal) &&
            fieldName.Slice(end - 1, 3).Equals("ies", StringComparison.Ordinal) &&
                typeName[..(end - 1)].Equals(fieldName[..(end - 1)], StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        
        return true;
    }

    private static bool DoesTypeNameMatch(ReadOnlySpan<char> typeName, ReadOnlySpan<char> fieldTypeName)
    {
        if (fieldTypeName.Equals(typeName, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (fieldTypeName.Length - 1 == typeName.Length)
        {
            if (fieldTypeName[^1].Equals('s') && 
                fieldTypeName[..^1].Equals(typeName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        if (typeName.EndsWith("y", StringComparison.Ordinal) && 
            fieldTypeName.Length - 2 == typeName.Length)
        {
            if(fieldTypeName.EndsWith("ies", StringComparison.Ordinal) &&
                fieldTypeName[..^3].Equals(typeName[..^1], StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static void TryRegisterEntityResolver(
        CompositionContext context,
        OutputField entityResolverField,
        OutputField entityResolverTargetField,
        ObjectType entityType,
        OutputField keyField,
        Schema schema)
    {
        if (!TryResolveKeyArgument(entityResolverField, keyField, context.FusionTypes, out var keyArg))
        {
            return;
        }

        if (entityResolverField.Type == entityType ||
            (entityResolverField.Type.Kind is TypeKind.NonNull &&
                entityResolverField.Type.InnerType() == entityType))
        {
            context.EntityResolverInfos.Add(
                new EntityResolverInfo(
                    entityType.Name,
                    entityType.GetOriginalName(),
                    ResolverKind.Fetch,
                    entityResolverTargetField,
                    new EntitySourceField(schema, entityResolverField),
                    new List<EntitySourceArgument>
                    {
                        new(keyArg, new IsDirective(keyField.Name))
                    }));
        }
    }
    
    private static void TryRegisterBatchEntityResolver(
        CompositionContext context,
        OutputField entityResolverField,
        OutputField entityResolverTargetField,
        ObjectType entityType,
        OutputField keyField,
        Schema schema)
    {
        if (!TryResolveBatchKeyArgument(entityResolverField, keyField, context.FusionTypes, out var keyArg))
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
            context.EntityResolverInfos.Add(
                new EntityResolverInfo(
                    entityType.Name,
                    entityType.GetOriginalName(),
                    ResolverKind.Batch,
                    entityResolverTargetField,
                    new EntitySourceField(schema, entityResolverField),
                    new List<EntitySourceArgument>
                    {
                        new(keyArg, new IsDirective(keyField.Name))
                    }));
        }
    }
    
    private static bool TryResolveKeyArgument(
        OutputField entityResolverField,
        OutputField keyField,
        IFusionTypeContext context,
        [NotNullWhen(true)] out InputField? keyArgument)
    {
        if (entityResolverField.Arguments.TryGetField(keyField.Name, out keyArgument))
        {
            return !IsDirective.ExistsIn(keyArgument, context) &&
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
            !IsDirective.ExistsIn(keyArgument, context);
    }

    private static bool TryResolveBatchKeyArgument(
        OutputField entityResolverField,
        OutputField keyField,
        IFusionTypeContext context,
        [NotNullWhen(true)] out InputField? keyArgument)
    {
        if (entityResolverField.Arguments.TryGetField(keyField.Name, out keyArgument))
        {
            if (keyArgument.Type.IsListType() && !IsDirective.ExistsIn(keyArgument, context))
            {
                return keyArgument.Type.ElementType().Equals(keyField.Type, TypeComparison.Structural);
            }

            keyArgument = null;
            return false;
        }

        if (entityResolverField.Arguments.Count == 1)
        {
            keyArgument = entityResolverField.Arguments.First();

            if (keyArgument.Type.IsListType() && !IsDirective.ExistsIn(keyArgument, context))
            {
                return keyArgument.Type.ElementType().Equals(keyField.Type, TypeComparison.Structural);
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

        if (keyArgument?.Type.IsListType() is true &&
            keyArgument.Type.ElementType().Equals(keyField.Type, TypeComparison.Structural) &&
            !IsDirective.ExistsIn(keyArgument, context))
        {
            return true;
        }

        keyArgument = null;
        return false;
    }
}