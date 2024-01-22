using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.Composition.Pipeline;

/// <summary>
/// A type handler that is responsible for merging enum types into a single distributed enum
/// type on the fusion graph.
/// </summary>
internal sealed class EnumTypeMergeHandler : ITypeMergeHandler
{
    /// <inheritdoc />
    public ValueTask<MergeStatus> MergeAsync(
        CompositionContext context,
        TypeGroup typeGroup,
        CancellationToken cancellationToken)
    {
        // If any type in the group is not an enum type, skip merging
        if (typeGroup.Parts.Any(t => t.Type.Kind is not TypeKind.Enum))
        {
            return new(MergeStatus.Skipped);
        }

        // Get the target enum type from the fusion graph
        var target = (EnumType)context.FusionGraph.Types[typeGroup.Name];

        // Merge each part of the enum type into the target enum type
        foreach (var part in typeGroup.Parts)
        {
            var source = (EnumType)part.Type;
            MergeType(context, source, part.Schema, target);
        }

        return new(MergeStatus.Completed);
    }

    private static void MergeType(
        CompositionContext context,
        EnumType source,
        Schema sourceSchema,
        EnumType target)
    {
        // Try to apply the source enum type to the target enum type
        context.TryApplySource(source, sourceSchema, target);

        // If the target enum type doesn't have a description, use the source enum type's
        // description
        target.MergeDescriptionWith(source);

        // Merge each value of the enum type
        foreach (var sourceValue in source.Values)
        {
            if (!target.Values.TryGetValue(sourceValue.Name, out var targetValue))
            {
                // If the target enum type doesn't have a value with the same name as the
                // source value, create a new target value with the source value's name
                targetValue = new EnumValue(sourceValue.Name);
                target.Values.Add(targetValue);
            }

            targetValue.MergeDescriptionWith(sourceValue);

            // If the source value is deprecated and the target value isn't, use the source
            // value's deprecation reason
            targetValue.MergeDeprecationWith(sourceValue);
            
            // Apply the source value to the target value
            context.ApplySource(sourceValue, sourceSchema, targetValue);
        }
    }
}
