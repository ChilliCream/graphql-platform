using System;
using System.Collections.Generic;
using HotChocolate.Types;

namespace HotChocolate.Data.Sorting
{
    public abstract class SortVisitorContext<T>
        : ISortVisitorContext<T>
    {
        protected SortVisitorContext(
            ISortInputType initialType)
        {
            if (initialType is null)
            {
                throw new ArgumentNullException(nameof(initialType));
            }

            Types.Push(initialType);
        }

        public Stack<IType> Types { get; } = new Stack<IType>();

        public Stack<IInputField> Fields { get; } = new Stack<IInputField>();

        public IList<IError> Errors { get; } = new List<IError>();

        public Queue<T> Operations { get; } = new Queue<T>();

        public Stack<T> Instance { get; } = new Stack<T>();
    }
}
