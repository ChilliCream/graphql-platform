using System;
using System.Collections.Generic;

namespace HotChocolate.Types.Sorting
{
    [Obsolete("Use HotChocolate.Data.")]
    public class SortVisitorContextBase
        : ISortVisitorContextBase
    {
        protected SortVisitorContextBase(InputObjectType initialType)
        {
            if (initialType is null)
            {
                throw new ArgumentNullException(nameof(initialType));
            }
            Types.Push(initialType);
        }

        public Stack<IType> Types { get; } =
            new Stack<IType>();

        public Stack<IInputField> Operations { get; } =
            new Stack<IInputField>();
    }
}
