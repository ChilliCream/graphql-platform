using System;
using System.Collections;
using System.Collections.Generic;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal class ListFieldValueHandler
        : IFieldValueHandler
    {
        public void CompleteValue(
            IFieldValueCompletionContext context,
            Action<IFieldValueCompletionContext> nextHandler)
        {
            if (context.Type.IsListType())
            {
                IType elementType = context.Type.ElementType();
                bool isNonNullElement = elementType.IsNonNullType();
                elementType = elementType.InnerType();

                Action<object, int, List<object>> completeElement = (element, index, list) =>
                {
                    nextHandler?.Invoke(context.AsElementValueContext(
                        context.Path.Append(index), elementType,
                        element, item => list.Add(item)));
                };

                CompleteList(context, completeElement, isNonNullElement);
            }
            else
            {
                nextHandler?.Invoke(context);
            }
        }


        private void CompleteList(
            IFieldValueCompletionContext context,
            Action<object, int, List<object>> completeElement,
            bool isNonNullElement)
        {
            if (context.Value is IEnumerable enumerable)
            {
                int i = 0;
                List<object> list = new List<object>();
                foreach (object element in enumerable)
                {
                    if (isNonNullElement && element == null)
                    {
                        context.AddError(
                            "The list does not allow null elements");
                        return;
                    }
                    completeElement(element, i++, list);
                }
                context.SetResult(list);
            }
            else
            {
                context.AddError(
                    "A list value must implement " +
                    $"{typeof(IEnumerable).FullName}.");
            }
        }
    }
}
