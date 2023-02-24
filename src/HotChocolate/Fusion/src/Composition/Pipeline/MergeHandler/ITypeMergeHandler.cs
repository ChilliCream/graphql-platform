using System.Diagnostics.Tracing;
using HotChocolate.Fusion.Composition.Types;
using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.Composition;

public interface ITypeMergeHandler
{
    MergeStatus Merge(
        CompositionContext context,
        TypeGroup typeGroup);
}

public sealed class ScalarMergeHandler : ITypeMergeHandler
{
    public MergeStatus Merge(CompositionContext context, TypeGroup typeGroup)
    {
        if (typeGroup.Parts.Any(t => t.Type.Kind is not TypeKind.Scalar))
        {
            return MergeStatus.Skipped;
        }

        var target = context.FusionGraph.Types[typeGroup.Name];

        foreach (var part in typeGroup.Parts)
        {
            var source = (ScalarType)part.Type;
            if(string.IsNullOrEmpty(target.Description))
            {
                target.Description = source.Description;
                break;
            }
        }

        return MergeStatus.Completed;
    }
}
