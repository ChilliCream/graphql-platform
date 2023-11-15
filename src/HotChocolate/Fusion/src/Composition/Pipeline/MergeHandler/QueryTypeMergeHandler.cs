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
internal sealed class QueryTypeMergeHandler : ITypeMergeHandler
{
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
            MergeType(context, source, part.Schema, target, context.FusionGraph);
        }

        return MergeStatus.Completed;
    }

    private static void MergeType(
        CompositionContext context,
        ObjectType source,
        Schema sourceSchema,
        ObjectType target,
        Schema targetSchema)
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
                if (!TryExtractAnnotationBasedEntityResolver(context, sourceSchema, sourceField, targetField))
                {
                    TryExtractNameBasedEntityResolver(context, sourceSchema, sourceField, targetField);
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
    
    private static void TryExtractNameBasedEntityResolver(
        CompositionContext context,
        Schema sourceSchema, 
        OutputField sourceField, 
        OutputField targetField)
    {
        if (sourceField.Arguments.Count == 0)
        {
            return;
        }

        List<EntitySourceArgument>? arguments = null;
        var kind = ResolverKind.Fetch;
        
        foreach (var argument in sourceField.Arguments)
        {
            if (kind is ResolverKind.Fetch && argument.Type.IsListType())
            {
                kind = ResolverKind.Batch;
            }
            
            if (!IsDirective.ExistsIn(argument, context.FusionTypes))
            {
                return;
            }

            var directive = IsDirective.TryGetFrom(argument, context.FusionTypes);
            
            if (directive is null)
            {
                // TODO : ERROR
                context.Log.Write(
                    new LogEntry(
                        "The is directive must have a value for coordinate or field.",
                        severity: LogSeverity.Error));
                return;
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
    }
}