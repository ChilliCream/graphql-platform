using System;
using System.Collections.Generic;

namespace HotChocolate.Types.Filters
{
    [Obsolete("Use HotChocolate.Data.")]
    public class FilterVisitorContextBase
        : IFilterVisitorContextBase
    {
        protected FilterVisitorContextBase(
            InputObjectType initialType)
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
