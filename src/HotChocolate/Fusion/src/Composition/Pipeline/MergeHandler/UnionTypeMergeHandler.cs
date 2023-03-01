using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.Composition.Pipeline;

public sealed class UnionTypeMergeHandler : ITypeMergeHandler
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

        var target = (UnionType)context.FusionGraph.Types[typeGroup.Name];

        foreach (var part in typeGroup.Parts)
        {
            var source = (UnionType)part.Type;
            MergeType(context, source, target, context.FusionGraph);
        }

        return new(MergeStatus.Completed);
    }

    private static void MergeType(
        CompositionContext context,
        UnionType source,
        UnionType target,
        Schema targetSchema)
    {
        if (string.IsNullOrEmpty(target.Description))
        {
            target.Description = source.Description;
        }

        foreach (var sourceType in source.Types)
        {
            var targetMemberType = (ObjectType)targetSchema.Types[sourceType.Name];

            if (!target.Types.Contains(targetMemberType))
            {
                target.Types.Add(targetMemberType);
            }
        }
    }
}
