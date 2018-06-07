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

        private readonly Action<FieldValueCompletionContext> _completeValue;

        public FieldValueCompleter()
        {
            Action<FieldValueCompletionContext> completeValue = null;
            for (int i = _handlers.Length - 1; i >= 0; i--)
            {
                completeValue = CreateValueCompleter(_handlers[i], completeValue);
            }
            _completeValue = completeValue;
        }

        private static Action<FieldValueCompletionContext> CreateValueCompleter(
            IFieldValueHandler handler,
            Action<FieldValueCompletionContext> completeValue)
        {
            return c => handler.CompleteValue(c, completeValue);
        }

        public void CompleteValue(FieldValueCompletionContext context)
        {
            _completeValue(context);
        }
    }
}
