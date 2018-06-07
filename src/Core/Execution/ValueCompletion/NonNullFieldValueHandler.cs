using System;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal class NonNullFieldValueHandler
        : IFieldValueHandler
    {
        public void CompleteValue(
            FieldValueCompletionContext context,
            Action<FieldValueCompletionContext> nextHandler)
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
