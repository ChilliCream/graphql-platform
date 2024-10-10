using HotChocolate.Skimmed;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Composition.Pipeline;

/// <summary>
/// Defines a type handler that is responsible for merging a group of directive types
/// into a single distributed directive type on the fusion graph.
/// </summary>
internal sealed class DirectiveTypeMergeHandler : ITypeMergeHandler
{
    /// <inheritdoc />
    public ValueTask<MergeStatus> MergeAsync(
        CompositionContext context,
        TypeGroup typeGroup,
        CancellationToken cancellationToken)
    {
        // Skip the merge operation if any part is not a scalar type.
        if (typeGroup.Parts.Any(t => t.Type.Kind is not TypeKind.Directive))
        {
            return new(MergeStatus.Skipped);
        }

        // Get the target directive type.
        var target = context.FusionGraph.Types[typeGroup.Name];

        // Merge each part of the scalar type.
        foreach (var part in typeGroup.Parts)
        {
            var source = (DirectiveTypeDefinition)part.Type;

            // Try to apply the source directive type to the target directive type.
            context.TryApplySource(source, part.Schema, target);

            // If the target directive type has no description,
            // set it to the source directive type's description.
            target.MergeDescriptionWith(source);
        }

        return new(MergeStatus.Completed);
    }
}
