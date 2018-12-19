using System;
using System.Collections.Generic;

namespace HotChocolate.Execution
{
    internal class QueryErrorFieldValueHandler
        : IFieldValueHandler
    {
        public void CompleteValue(
            IFieldValueCompletionContext completionContext,
            Action<IFieldValueCompletionContext> nextHandler)
        {
            if (completionContext.Value is IError error)
            {
                completionContext.ReportError(error);
            }
            else if (completionContext.Value is IEnumerable<IError> errors)
            {
                completionContext.ReportError(errors);
            }
            else
            {
                nextHandler?.Invoke(completionContext);
            }
        }
    }
}
