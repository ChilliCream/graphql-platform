using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal abstract partial class ExecutionStrategyBase
    {
        private static readonly FieldValueCompleter _fieldValueCompleter =
            new FieldValueCompleter();

        protected static async Task<object> ExecuteResolverAsync(
           ResolverTask resolverTask,
           CancellationToken cancellationToken)
        {
            Activity activity = DiagnosticEvents.BeginResolveField(
                resolverTask.ResolverContext);

            object result = await ExecuteMiddlewareAsync(
                resolverTask,
                cancellationToken);

            if (result is IQueryError error)
            {
                activity.AddTag("error", "true");
            }

            DiagnosticEvents.EndResolveField(
                activity,
                resolverTask.ResolverContext,
                result);

            return result;
        }

        private static async Task<object> ExecuteMiddlewareAsync(
            ResolverTask resolverTask,
            CancellationToken cancellationToken)
        {
            object result = null;

            try
            {
                if (resolverTask.HasMiddleware)
                {
                    result = await ExecuteDirectiveMiddlewareAsync(
                        resolverTask,
                        cancellationToken);
                }
                else
                {
                    result = await ExecuteFieldMiddlewareAsync(
                        resolverTask,
                        cancellationToken);
                }
            }
            catch (QueryException ex)
            {
                result = ex.Errors;
            }
            catch (Exception ex)
            {
                DiagnosticEvents.ResolverError(
                    resolverTask.ResolverContext,
                    ex);

                result = resolverTask.CreateError(ex);
            }

            return result;
        }

        private static async Task<object> ExecuteFieldMiddlewareAsync(
            ResolverTask resolverTask,
            CancellationToken cancellationToken)
        {
            if (resolverTask.FieldSelection.Field.Resolver == null)
            {
                return null;
            }

            object result = await resolverTask.FieldSelection.Field.Resolver(
                resolverTask.ResolverContext,
                cancellationToken);

            return resolverTask.CompleteResolverResult(result);
        }

        private static async Task<object> ExecuteDirectiveMiddlewareAsync(
            ResolverTask resolverTask,
            CancellationToken cancellationToken)
        {
            return await resolverTask.ExecuteMiddleware.Invoke(
                resolverTask.ResolverContext, ExecuteResolver);

            Task<object> ExecuteResolver()
            {
                return ExecuteFieldMiddlewareAsync(
                    resolverTask,
                    cancellationToken);
            };
        }

        protected void CompleteValue(
            FieldValueCompletionContext completionContext)
        {
            _fieldValueCompleter.CompleteValue(completionContext);
        }
    }
}
