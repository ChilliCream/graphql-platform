using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.Composition.Pipeline;

public sealed class EnumTypeMergeHandler : ITypeMergeHandler
{
    public ValueTask<MergeStatus> MergeAsync(
        CompositionContext context,
        TypeGroup typeGroup,
        CancellationToken cancellationToken)
    {
        if (typeGroup.Parts.Any(t => t.Type.Kind is not TypeKind.Union))
        {
            return new(MergeStatus.Skipped);
        }

        var target = (EnumType)context.FusionGraph.Types[typeGroup.Name];

        foreach (var part in typeGroup.Parts)
        {
            var source = (EnumType)part.Type;
            MergeType(context, source, target);
        }

        return new(MergeStatus.Completed);
    }

    private static void MergeType(
        CompositionContext context,
        EnumType source,
        EnumType target)
    {
        if (string.IsNullOrEmpty(target.Description))
        {
            target.Description = source.Description;
        }

        foreach (var sourceValue in source.Values)
        {
            if (!target.Values.TryGetValue(sourceValue.Name, out var targetValue))
            {
                targetValue = new EnumValue(source.Name);
                target.Values.Add(targetValue);
            }

            if (sourceValue.IsDeprecated && string.IsNullOrEmpty(targetValue.DeprecationReason))
            {
                sourceValue.DeprecationReason = targetValue.DeprecationReason;
            }
        }
    }
}
