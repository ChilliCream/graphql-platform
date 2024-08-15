using HotChocolate.Resolvers;

#nullable enable

namespace HotChocolate.Types.Pagination;

/// <summary>
/// The paging handler is used in the paging middleware to abstract the actual paging logic.
/// </summary>
public interface IPagingHandler
{
    /// <summary>
    /// Will be called by the paging middleware before anything is executed and validates
    /// if the current context is valid.
    /// </summary>
    /// <param name="context">
    /// The current resolver context.
    /// </param>
    /// <exception cref="GraphQLException">
    /// If context is not valid a <see cref="GraphQLException"/> is expected.
    /// </exception>
    void ValidateContext(IResolverContext context);

    /// <summary>
    /// Publish Paging Arguments to local state.
    /// </summary>
    /// <param name="context">
    /// The current resolver context.
    /// </param>
    void PublishPagingArguments(IResolverContext context);

    /// <summary>
    /// Slices the <paramref name="source"/> and returns a page from it.
    /// </summary>
    /// <param name="context">
    /// The current resolver context.
    /// </param>
    /// <param name="source">
    /// The data set.
    /// </param>
    /// <returns>
    /// Returns the page representing a part from the <paramref name="source"/>.
    /// </returns>
    ValueTask<IPage> SliceAsync(IResolverContext context, object source);
}
