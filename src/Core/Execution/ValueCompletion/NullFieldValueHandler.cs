using System;

namespace HotChocolate.Execution
{
    internal class NullFieldValueHandler
        : IFieldValueHandler
    {
        public void CompleteValue(
            IFieldValueCompletionContext context,
            Action<IFieldValueCompletionContext> nextHandler)
        {
            if (context.Value == null)
            {
                context.IntegrateResult(null);
            }
            else
            {
                nextHandler?.Invoke(context);
            }
        }
    }
}
