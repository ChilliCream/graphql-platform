using System;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal class NonNullFieldValueHandler
        : IFieldValueHandler
    {
        public void CompleteValue(
            IFieldValueCompletionContext context,
            Action<IFieldValueCompletionContext> nextHandler)
        {
            if (context.Type.IsNonNullType())
            {
                nextHandler?.Invoke(context.AsNonNullValueContext());
            }
            else
            {
                nextHandler?.Invoke(context);
            }
        }
    }
}
