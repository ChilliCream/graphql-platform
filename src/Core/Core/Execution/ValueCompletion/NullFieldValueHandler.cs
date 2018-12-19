using System;

namespace HotChocolate.Execution
{
    internal class NullFieldValueHandler
        : IFieldValueHandler
    {
        public void CompleteValue(
            IFieldValueCompletionContext completionContext,
            Action<IFieldValueCompletionContext> nextHandler)
        {
            if (completionContext.Value == null)
            {
                completionContext.IntegrateResult(null);
            }
            else
            {
                nextHandler?.Invoke(completionContext);
            }
        }
    }
}
