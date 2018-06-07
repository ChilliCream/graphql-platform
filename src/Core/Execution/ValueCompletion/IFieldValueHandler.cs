using System;

namespace HotChocolate.Execution
{
    internal interface IFieldValueHandler
    {
        void CompleteValue(
            FieldValueCompletionContext context,
            Action<FieldValueCompletionContext> nextHandler);
    }
}
