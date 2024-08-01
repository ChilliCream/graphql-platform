using HotChocolate.Execution.Processing;
using HotChocolate.Resolvers;

namespace HotChocolate.Execution.Internal;

/// <summary>
/// An unsafe class that provides a set of methods to access the
/// underlying data representations of the middleware context.
/// </summary>
public static class MiddlewareContextMarshal
{
    /// <summary>
    /// Gets access to the result data of an object in the GraphQL execution.
    /// ResultData is pooled and writing to it can corrupt the result.
    /// Multiple threads might be writing into the result object.
    /// </summary>
    /// <param name="context">
    /// The resolver context.
    /// </param>
    /// <returns>
    /// Returns the result data of the current resolver context.
    /// </returns>
    public static ObjectResult? GetParentResultUnsafe(IResolverContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        return context is MiddlewareContext middlewareContext
            ? middlewareContext.ParentResult
            : null;
    }

    /// <summary>
    /// Gets the parent result data of the current <paramref name="resultData"/>.
    /// </summary>
    /// <param name="resultData">
    /// The result data for which to get the parent.
    /// </param>
    /// <typeparam name="T">
    /// The type of the result data.
    /// </typeparam>
    /// <returns>
    /// Returns the parent result data of the current <paramref name="resultData"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Throws if <paramref name="resultData"/> is <c>null</c>.
    /// </exception>
    public static ResultData? GetParent<T>(T resultData) where T : ResultData
    {
        if (resultData == null)
        {
            throw new ArgumentNullException(nameof(resultData));
        }

        return resultData.Parent;
    }

    /// <summary>
    /// Gets the index under which the <paramref name="resultData"/> is stored in the parent result.
    /// </summary>
    /// <param name="resultData">
    /// The result data for which to get the parent index.
    /// </param>
    /// <typeparam name="T">
    /// The type of the result data.
    /// </typeparam>
    /// <returns>
    /// Returns the index under which the <paramref name="resultData"/> is stored in the parent result.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Throws if <paramref name="resultData"/> is <c>null</c>.
    /// </exception>
    public static int GetParentIndex<T>(T resultData) where T : ResultData
    {
        if (resultData == null)
        {
            throw new ArgumentNullException(nameof(resultData));
        }

        return resultData.ParentIndex;
    }
}
