using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Language;

namespace HotChocolate.Execution
{
    internal abstract partial class ExecutionStrategyBase
    {
        protected async Task<object> FinalizeResolverResultAsync(
            FieldNode fieldSelection, object resolverResult,
            bool isDeveloperMode)
        {
            switch (resolverResult)
            {
                case Task<object> task:
                    return await FinalizeResolverResultTaskAsync(
                        fieldSelection, task, isDeveloperMode);
                default:
                    return resolverResult;
            }
        }

        private async Task<object> FinalizeResolverResultTaskAsync(
            FieldNode fieldSelection, Task<object> task,
            bool isDeveloperMode)
        {
            try
            {
                return await task;
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
            Exception exception, FieldNode fieldSelection,
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

        protected void CompleteValue(FieldValueCompletionContext completionContext)
        {
            // _valueCompleter.CompleteValue(completionContext);
        }

    }
}
