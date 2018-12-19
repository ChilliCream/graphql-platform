using System;

namespace HotChocolate.Execution
{
    internal interface IFieldValueHandler
    {
        void CompleteValue(
            IFieldValueCompletionContext completionContext,
            Action<IFieldValueCompletionContext> nextHandler);
    }
}
