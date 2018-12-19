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
            IFieldValueCompletionContext completionContext,
            Action<IFieldValueCompletionContext> nextHandler)
        {
            if (completionContext.Type.IsListType())
            {
                IType elementType = completionContext.Type.ElementType();
                bool isNonNullElement = elementType.IsNonNullType();
                elementType = elementType.InnerType();

                Action<object, int, List<object>> completeElement =
                    (element, index, list) =>
                    {
                        nextHandler?.Invoke(
                            completionContext.AsElementValueContext(
                            completionContext.Path.Append(index), elementType,
                            element, item => list.Add(item)));
                    };

                CompleteList(
                    completionContext,
                    completeElement,
                    isNonNullElement);
            }
            else
            {
                nextHandler?.Invoke(completionContext);
            }
        }

        private void CompleteList(
            IFieldValueCompletionContext completionContext,
            Action<object, int, List<object>> completeElement,
            bool isNonNullElement)
        {
            if (completionContext.Value is IEnumerable enumerable)
            {
                CompleteList(
                    completionContext,
                    completeElement,
                    isNonNullElement,
                    enumerable);
            }
            else
            {
                completionContext.ReportError(
                    "A list value must implement " +
                    $"{typeof(IEnumerable).FullName}.");
            }
        }

        private void CompleteList(
            IFieldValueCompletionContext completionContext,
            Action<object, int, List<object>> completeElement,
            bool isNonNullElement,
            IEnumerable enumerable)
        {
            int i = 0;
            var list = new List<object>();
            foreach (object element in enumerable)
            {
                if (element == null)
                {
                    if (isNonNullElement)
                    {
                        completionContext.ReportError(
                            "The list does not allow null elements");
                        return;
                    }
                    else
                    {
                        i++;
                        list.Add(null);
                    }
                }
                else
                {
                    completeElement(element, i++, list);
                }
            }
            completionContext.IntegrateResult(list);
        }
    }
}
