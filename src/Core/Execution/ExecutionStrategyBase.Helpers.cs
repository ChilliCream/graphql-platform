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
           bool isDeveloperMode,
           CancellationToken cancellationToken)
        {
            Activity activity = DiagnosticEvents.BeginResolveField(
                resolverTask.ResolverContext);

            object result = await ExecuteResolverInternalAsync(
                resolverTask,
                isDeveloperMode,
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

        private static async Task<object> ExecuteResolverInternalAsync(
            ResolverTask resolverTask,
            bool isDeveloperMode,
            CancellationToken cancellationToken)
        {
            object result = null;

            try
            {
                if (resolverTask.HasMiddleware)
                {
                    result = await ExecuteResolverMiddlewareAsync(
                        resolverTask, isDeveloperMode,
                        cancellationToken);
                }
                else
                {
                    result = await ExecuteFieldResolverAsync(
                        resolverTask, isDeveloperMode,
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

                result = CreateErrorFromException(ex,
                    resolverTask.FieldSelection.Selection,
                    isDeveloperMode);
            }

            return result;
        }

        private static async Task<object> ExecuteFieldResolverAsync(
            ResolverTask resolverTask,
            bool isDeveloperMode,
            CancellationToken cancellationToken)
        {
            if (resolverTask.FieldSelection.Field.Resolver == null)
            {
                return null;
            }

            object result = await resolverTask.FieldSelection.Field.Resolver(
                resolverTask.ResolverContext,
                cancellationToken);

            return FinalizeResolverResult(
                resolverTask.FieldSelection.Selection,
                result,
                isDeveloperMode);
        }

        private static async Task<object> ExecuteResolverMiddlewareAsync(
            ResolverTask resolverTask,
            bool isDeveloperMode,
            CancellationToken cancellationToken)
        {
            return await resolverTask.ExecuteMiddleware.Invoke(
                resolverTask.ResolverContext, ExecuteResolver);

            Task<object> ExecuteResolver()
            {
                return ExecuteFieldResolverAsync(
                    resolverTask,
                    isDeveloperMode,
                    cancellationToken);
            };
        }

        protected static object FinalizeResolverResult(
            FieldNode fieldSelection,
            object resolverResult,
            bool isDeveloperMode)
        {
            switch (resolverResult)
            {
                case IResolverResult result:
                    return CompleteResolverResult(fieldSelection, result);

                default:
                    return resolverResult;
            }
        }

        private static IQueryError CreateErrorFromException(
            Exception exception,
            FieldNode fieldSelection,
            bool isDeveloperMode)
        {
            if (isDeveloperMode)
            {
                return new FieldError(
                    $"{exception.Message}\r\n\r\n{exception.StackTrace}",
                    fieldSelection);
            }
            else
            {
                return new FieldError(
                    "Unexpected execution error.",
                    fieldSelection);
            }
        }

        protected void CompleteValue(
            FieldValueCompletionContext completionContext)
        {
            _fieldValueCompleter.CompleteValue(completionContext);
        }

        private static object CompleteResolverResult(
            FieldNode fieldSelection,
            IResolverResult resolverResult)
        {
            if (resolverResult.IsError)
            {
                return new FieldError(
                    resolverResult.ErrorMessage,
                    fieldSelection);
            }
            return resolverResult.Value;
        }

        private static bool IsMaxExecutionDepthReached(
            IExecutionContext executionContext,
            ResolverTask resolverTask)
        {
            bool isLeafField =
                resolverTask.FieldSelection.Field.Type.IsLeafType();

            int maxExecutionDepth = isLeafField
                ? executionContext.Options.MaxExecutionDepth
                : executionContext.Options.MaxExecutionDepth - 1;

            return resolverTask.Path.Depth > maxExecutionDepth;
        }
    }
}
