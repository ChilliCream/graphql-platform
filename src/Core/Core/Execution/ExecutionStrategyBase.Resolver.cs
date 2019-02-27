using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal abstract partial class ExecutionStrategyBase
    {
        protected static async Task<object> ExecuteResolverAsync(
           ResolverTask resolverTask,
           IErrorHandler errorHandler,
           CancellationToken cancellationToken)
        {
            Activity activity = resolverTask.Diagnostics.BeginResolveField(
                resolverTask.ResolverContext);

            object result = await ExecuteMiddlewareAsync(
                resolverTask, errorHandler)
                .ConfigureAwait(false);

            if (result is IEnumerable<IError> errors)
            {
                resolverTask.Diagnostics.ResolverError(
                    resolverTask.ResolverContext, errors);
            }
            else if (result is IError error)
            {
                resolverTask.Diagnostics.ResolverError(
                    resolverTask.ResolverContext, error);
            }

            resolverTask.Diagnostics.EndResolveField(
                activity,
                resolverTask.ResolverContext,
                result);

            return result;
        }

        private static async Task<object> ExecuteMiddlewareAsync(
            ResolverTask resolverTask,
            IErrorHandler errorHandler)
        {
            object result = null;

            try
            {
                result = await ExecuteFieldMiddlewareAsync(resolverTask)
                    .ConfigureAwait(false);

                if (result is IError error)
                {
                    return errorHandler.Handle(error);
                }
                else if (result is IEnumerable<IError> errors)
                {
                    return errorHandler.Handle(errors);
                }
                else
                {
                    return result;
                }
            }
            catch (QueryException ex)
            {
                return errorHandler.Handle(ex.Errors);
            }
            catch (Exception ex)
            {
                return errorHandler.Handle(ex, builder => builder
                    .SetPath(resolverTask.Path)
                    .AddLocation(resolverTask.FieldSelection.Selection));
            }
        }

        private static async Task<object> ExecuteFieldMiddlewareAsync(
            ResolverTask resolverTask)
        {
            var middlewareContext = new MiddlewareContext
            (
                resolverTask.ResolverContext,
                () => resolverTask.FieldSelection.Field
                    .Resolver?.Invoke(resolverTask.ResolverContext)
                        ?? Task.FromResult<object>(null),
                result => resolverTask.CompleteResolverResult(result)
            );

            await resolverTask.FieldDelegate.Invoke(middlewareContext)
                .ConfigureAwait(false);

            return middlewareContext.Result;
        }
    }
}
