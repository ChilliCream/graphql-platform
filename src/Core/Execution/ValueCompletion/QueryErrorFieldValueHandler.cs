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
            if (completionContext.Value is IQueryError error)
            {
                completionContext.ReportError(error);
            }
            else if (completionContext.Value is IEnumerable<IQueryError> errors)
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
