using System;

namespace HotChocolate.Execution
{
    internal class FieldValueCompleter
    {
        private static readonly IFieldValueHandler[] _handlers = new IFieldValueHandler[]
        {
            new QueryErrorFieldValueHandler(),
            new NonNullFieldValueHandler(),
            new NullFieldValueHandler(),
            new ListFieldValueHandler(),
            new ScalarFieldValueHandler(),
            new ObjectFieldValueHandler()
        };

        private readonly Action<IFieldValueCompletionContext> _completeValue;

        public FieldValueCompleter()
        {
            Action<IFieldValueCompletionContext> completeValue = null;
            for (int i = _handlers.Length - 1; i >= 0; i--)
            {
                completeValue = CreateValueCompleter(_handlers[i], completeValue);
            }
            _completeValue = completeValue;
        }

        private static Action<IFieldValueCompletionContext> CreateValueCompleter(
            IFieldValueHandler valueHandler,
            Action<IFieldValueCompletionContext> completeValue)
        {
            return c => valueHandler.CompleteValue(c, completeValue);
        }

        public void CompleteValue(IFieldValueCompletionContext completionContext)
        {
            _completeValue(completionContext);
        }
    }
}
