using System;
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
    /// <returns></returns>
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
}