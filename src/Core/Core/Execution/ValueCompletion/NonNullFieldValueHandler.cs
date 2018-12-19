using System;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal class NonNullFieldValueHandler
        : IFieldValueHandler
    {
        public void CompleteValue(
            IFieldValueCompletionContext completionContext,
            Action<IFieldValueCompletionContext> nextHandler)
        {
            if (completionContext.Type.IsNonNullType())
            {
                nextHandler?.Invoke(completionContext.AsNonNullValueContext());
            }
            else
            {
                nextHandler?.Invoke(completionContext);
            }
        }
    }
}
