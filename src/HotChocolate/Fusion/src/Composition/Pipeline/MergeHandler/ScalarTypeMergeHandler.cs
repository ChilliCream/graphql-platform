using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.Composition.Pipeline;

public sealed class ScalarTypeMergeHandler : ITypeMergeHandler
{
    public ValueTask<MergeStatus> MergeAsync(
        CompositionContext context,
        TypeGroup typeGroup,
        CancellationToken cancellationToken)
    {
        if (typeGroup.Parts.Any(t => t.Type.Kind is not TypeKind.Scalar))
        {
            return new(MergeStatus.Skipped);
        }

        var target = context.FusionGraph.Types[typeGroup.Name];

        foreach (var part in typeGroup.Parts)
        {
            var source = (ScalarType)part.Type;

            if (string.IsNullOrEmpty(target.Description))
            {
                target.Description = source.Description;
                break;
            }
        }

        return new(MergeStatus.Completed);
    }
}
