using System;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Language;

namespace HotChocolate.Execution
{
    internal abstract partial class ExecutionStrategyBase
    {
        private static readonly FieldValueCompleter _fieldValueCompleter =
            new FieldValueCompleter();

        protected static object ExecuteResolver(
           ResolverTask resolverTask,
           bool isDeveloperMode,
           CancellationToken cancellationToken)
        {
            try
            {
                return resolverTask.FieldSelection.Field.Resolver(
                    resolverTask.ResolverContext,
                    cancellationToken);
            }
            catch (QueryException ex)
            {
                return ex.Errors;
            }
            catch (Exception ex)
            {
                return CreateErrorFromException(ex,
                    resolverTask.FieldSelection.Selection,
                    isDeveloperMode);
            }
        }

        protected static async Task<object> FinalizeResolverResultAsync(
            FieldNode fieldSelection,
            object resolverResult,
            bool isDeveloperMode)
        {
            switch (resolverResult)
            {
                case Task<object> task:
                    return await FinalizeResolverResultTaskAsync(
                        fieldSelection, task, isDeveloperMode);

                case IResolverResult result:
                    return CompleteResolverResult(fieldSelection, result);

                default:
                    return resolverResult;
            }
        }

        private static async Task<object> FinalizeResolverResultTaskAsync(
            FieldNode fieldSelection,
            Task<object> task,
            bool isDeveloperMode)
        {
            try
            {
                object resolverResult = await task;
                if (resolverResult is IResolverResult r)
                {
                    return CompleteResolverResult(fieldSelection, r);
                }
                return resolverResult;
            }
            catch (QueryException ex)
            {
                return ex.Errors;
            }
            catch (Exception ex)
            {
                return CreateErrorFromException(ex,
                    fieldSelection, isDeveloperMode);
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
    }
}
