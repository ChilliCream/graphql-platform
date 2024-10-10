using HotChocolate.Skimmed;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Fusion.Composition.Pipeline;

/// <summary>
/// Defines a type handler that is responsible for merging a group of interface types
/// into a single distributed interface type on the fusion graph.
/// </summary>
internal sealed class InterfaceTypeMergeHandler : ITypeMergeHandler
{
    /// <inheritdoc />
    public ValueTask<MergeStatus> MergeAsync(
        CompositionContext context,
        TypeGroup typeGroup,
        CancellationToken cancellationToken)
    {
        // If the types in the group are not interface types, skip merging them.
        if (typeGroup.Parts.Any(t => t.Type.Kind is not TypeKind.Interface))
        {
            return new(MergeStatus.Skipped);
        }

        // Get the target interface type from the fusion graph.
        var target = (InterfaceTypeDefinition)context.FusionGraph.Types[typeGroup.Name];

        // Merge the parts of the interface type group into the target interface type.
        foreach (var part in typeGroup.Parts)
        {
            var source = (InterfaceTypeDefinition)part.Type;
            MergeType(context, source, part.Schema, target);
        }

        return new(MergeStatus.Completed);
    }

    private static void MergeType(
        CompositionContext context,
        InterfaceTypeDefinition source,
        SchemaDefinition sourceSchema,
        InterfaceTypeDefinition target)
    {
        // Apply the source type to the target type.
        context.TryApplySource(source, sourceSchema, target);

        // If the target type does not have a description, use the source type's description.
        target.MergeDescriptionWith(source);

        target.MergeDirectivesWith(source, context);

        // Add all of the interfaces that the source type implements to the target type.
        foreach (var interfaceType in source.Implements)
        {
            if (!target.Implements.Any(t => t.Name.EqualsOrdinal(interfaceType.Name)))
            {
                target.Implements.Add((InterfaceTypeDefinition)context.FusionGraph.Types[interfaceType.Name]);
            }
        }

        // Merge the fields of the source type into the target type.
        foreach (var sourceField in source.Fields)
        {
            if (target.Fields.TryGetField(sourceField.Name, out var targetField))
            {
                context.MergeField(sourceField, targetField, source.Name);
            }
            else
            {
                targetField = context.CreateField(sourceField, context.FusionGraph);
                target.Fields.Add(targetField);
            }

            context.TryApplySource(sourceField, sourceSchema, targetField);
        }
    }
}
