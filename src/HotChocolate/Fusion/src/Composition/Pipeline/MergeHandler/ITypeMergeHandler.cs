namespace HotChocolate.Fusion.Composition.Pipeline;

public interface ITypeMergeHandler
{
    ValueTask<MergeStatus> MergeAsync(
        CompositionContext context,
        TypeGroup typeGroup,
        CancellationToken cancellationToken = default);
}
