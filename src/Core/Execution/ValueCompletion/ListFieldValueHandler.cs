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
                int i = 0;
                IType elementType = context.Type.ElementType();
                bool isNonNullElement = elementType.IsNonNullType();
                elementType = elementType.InnerType();
                List<object> list = new List<object>();

                if (context.Value is IEnumerable enumerable)
                {
                    foreach (object element in enumerable)
                    {
                        if (isNonNullElement && element == null)
                        {
                            context.AddError(
                                "The list does not allow null elements");
                            return;
                        }

                        nextHandler?.Invoke(context.AsElementValueContext(
                            context.Path.Append(i++), elementType,
                            element, item => list.Add(item)));
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
            else
            {
                nextHandler?.Invoke(context);
            }
        }
    }
}
