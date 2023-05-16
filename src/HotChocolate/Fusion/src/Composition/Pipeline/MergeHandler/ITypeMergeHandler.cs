namespace HotChocolate.Fusion.Composition.Pipeline;

/// <summary>
/// Defines a type handler that is responsible for merging a group of types
/// into a single distributed type on the fusion graph.
/// </summary>
internal interface ITypeMergeHandler
{
    /// <summary>
    /// Merges a group of types into a single distributed type on the fusion graph
    /// </summary>
    /// <param name="context">The composition context.</param>
    /// <param name="typeGroup">The group of types to merge.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> that represents the asynchronous operation
    /// and returns the merge status.
    /// </returns>
    ValueTask<MergeStatus> MergeAsync(
        CompositionContext context,
        TypeGroup typeGroup,
        CancellationToken cancellationToken = default);
}
