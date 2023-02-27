using System.Diagnostics.Tracing;
using HotChocolate.Fusion.Composition.Types;

namespace HotChocolate.Fusion.Composition;

public interface ITypeMergeHandler
{
    ValueTask<MergeStatus> MergeAsync(
        CompositionContext context,
        TypeGroup typeGroup,
        CancellationToken cancellationToken = default);
}
