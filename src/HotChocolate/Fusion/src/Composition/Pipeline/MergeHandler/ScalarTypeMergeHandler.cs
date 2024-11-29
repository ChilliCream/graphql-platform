using HotChocolate.Skimmed;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Composition.Pipeline;

/// <summary>
/// Defines a type handler that is responsible for merging a group of scalar types
/// into a single distributed scalar type on the fusion graph.
/// </summary>
internal sealed class ScalarTypeMergeHandler : ITypeMergeHandler
{
    /// <inheritdoc />
    public ValueTask<MergeStatus> MergeAsync(
        CompositionContext context,
        TypeGroup typeGroup,
        CancellationToken cancellationToken)
    {
        // Skip the merge operation if any part is not a scalar type.
        if (typeGroup.Parts.Any(t => t.Type.Kind is not TypeKind.Scalar))
        {
            return new(MergeStatus.Skipped);
        }

        // Get the target scalar type.
        var target = context.FusionGraph.Types[typeGroup.Name];

        // Merge each part of the scalar type.
        foreach (var part in typeGroup.Parts)
        {
            var source = (ScalarTypeDefinition)part.Type;

            // Try to apply the source scalar type to the target scalar type.
            context.TryApplySource(source, part.Schema, target);

            target.MergeDirectivesWith(source, context);

            // If the target scalar type has no description,
            // set it to the source scalar type's description.
            target.MergeDescriptionWith(source);
        }

        return new(MergeStatus.Completed);
    }
}
