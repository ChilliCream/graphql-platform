using System;
using System.Collections.Generic;

namespace HotChocolate.Execution
{
    internal class QueryErrorFieldValueHandler
        : IFieldValueHandler
    {
        public void CompleteValue(
            IFieldValueCompletionContext context,
            Action<IFieldValueCompletionContext> nextHandler)
        {
            if (context.Value is IQueryError error)
            {
                context.AddError(error);
            }
            else if (context.Value is IEnumerable<IQueryError> errors)
            {
                context.AddErrors(errors);
            }
            else
            {
                nextHandler?.Invoke(context);
            }
        }
    }
}
