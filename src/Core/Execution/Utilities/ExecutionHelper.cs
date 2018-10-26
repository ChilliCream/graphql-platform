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
                    return new FieldError(
                        r.ErrorMessage,
                        resolverContext.FieldSelection);
                }
                return r.Value;
            }

            return resolverResult;
        }

        public static bool IsMaxExecutionDepthReached(
            this ResolverTask resolverTask)
        {
            bool isLeafField =
                resolverTask.FieldSelection.Field.Type.IsLeafType();

            int maxExecutionDepth = isLeafField
                ? resolverTask.Options.MaxExecutionDepth
                : resolverTask.Options.MaxExecutionDepth - 1;

            return resolverTask.Path.Depth > maxExecutionDepth;
        }

        public static FieldError CreateError(
            this ResolverTask resolverTask,
            Exception exception)
        {
            if (resolverTask.Options.DeveloperMode)
            {
                return resolverTask.CreateError(
                    $"{exception.Message}\r\n\r\n{exception.StackTrace}");
            }
            else
            {
                return resolverTask.CreateError("Unexpected execution error.");
            }
        }
    }

}
