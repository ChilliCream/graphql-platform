using HotChocolate.Skimmed;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Composition.Pipeline;

/// <summary>
/// A type handler that is responsible for merging input object types into a single distributed
/// input object type on the fusion graph.
/// </summary>
internal sealed class InputObjectTypeMergeHandler : ITypeMergeHandler
{
    /// <inheritdoc />
    public ValueTask<MergeStatus> MergeAsync(
        CompositionContext context,
        TypeGroup typeGroup,
        CancellationToken cancellationToken)
    {
        // If any type in the group is not an input object type, skip merging
        if (typeGroup.Parts.Any(t => t.Type.Kind is not TypeKind.InputObject))
        {
            return new(MergeStatus.Skipped);
        }

        // Get the target input object type from the fusion graph
        var target = (InputObjectTypeDefinition)context.FusionGraph.Types[typeGroup.Name];

        // Merge each part of the input object type into the target input object type
        foreach (var part in typeGroup.Parts)
        {
            var source = (InputObjectTypeDefinition)part.Type;
            MergeType(context, source, part.Schema, target, context.FusionGraph);
        }

        return new(MergeStatus.Completed);
    }

    private static void MergeType(
        CompositionContext context,
        InputObjectTypeDefinition source,
        SchemaDefinition sourceSchema,
        InputObjectTypeDefinition target,
        SchemaDefinition targetSchema)
    {
        // Try to apply the source input object type to the target input object type
        context.TryApplySource(source, sourceSchema, target);

        // If the target input object type doesn't have a description, use the source input
        // object type's description
        target.MergeDescriptionWith(source);
        
        target.MergeDirectivesWith(source, context);

        // Merge each field of the input object type
        foreach (var sourceField in source.Fields)
        {
            if (target.Fields.TryGetField(sourceField.Name, out var targetField))
            {
                // If the target input object type has a field with the same name as the source
                // field, merge the source field into the target field
                context.MergeField(source, sourceField, targetField);
            }
            else
            {
                // If the target input object type doesn't have a field with the same name as
                // the source field, create a new target field with the source field's
                // properties
                targetField = context.CreateField(sourceField, targetSchema);
                target.Fields.Add(targetField);
            }

            // Try to apply the source field to the target field
            context.TryApplySource(sourceField, sourceSchema, targetField);
        }
    }
}
