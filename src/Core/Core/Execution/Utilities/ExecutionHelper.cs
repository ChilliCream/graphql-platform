using System;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal static class ExecutionHelper
    {
        public static object CompleteResolverResult(
            this ResolverTask resolverTask,
            object resolverResult)
        {
            return CompleteResolverResult(
                resolverTask.ResolverContext,
                resolverResult);
        }

        public static object CompleteResolverResult(
            this IResolverContext resolverContext,
            object resolverResult)
        {
            if (resolverResult is IResolverResult r)
            {
                if (r.IsError)
                {
                    return QueryError.CreateFieldError(
                        r.ErrorMessage,
                        resolverContext.Path,
                        resolverContext.FieldSelection);
                }
                return r.Value;
            }

            return resolverResult;
        }
    }
}
