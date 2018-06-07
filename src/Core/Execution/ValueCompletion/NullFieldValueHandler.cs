using System;

namespace HotChocolate.Execution
{
    internal class NullFieldValueHandler
        : IFieldValueHandler
    {
        public void CompleteValue(
            FieldValueCompletionContext context,
            Action<FieldValueCompletionContext> nextHandler)
        {
            if (context.Value == null)
            {
                context.SetResult(null);
            }
            else
            {
                nextHandler?.Invoke(context);
            }
        }
    }
}
