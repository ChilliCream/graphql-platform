using HotChocolate.Skimmed;
using static HotChocolate.Fusion.Composition.DirectiveArguments;
using static HotChocolate.Fusion.Composition.WellKnownContextData;

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
        context.TryApplySource(source, sourceSchema, target);

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

            context.TryApplySource(sourceValue, sourceSchema, targetValue);

            if (sourceValue.IsDeprecated && string.IsNullOrEmpty(targetValue.DeprecationReason))
            {
                sourceValue.DeprecationReason = targetValue.DeprecationReason;
            }
        }
    }
}
