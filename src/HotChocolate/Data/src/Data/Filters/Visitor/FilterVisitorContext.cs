using System;
using System.Collections.Generic;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Data.Filters
{
    public abstract class FilterVisitorContext<T>
        : IFilterVisitorContext<T>
    {
        protected FilterVisitorContext(
            IFilterInputType initialType,
            FilterScope<T>? filterScope = null)
        {
            if (initialType is null)
            {
                throw new ArgumentNullException(nameof(initialType));
            }

            Types.Push(initialType);
            Scopes = new StackableList<FilterScope<T>>();
            Scopes.Push(filterScope ?? CreateScope());
        }

        public IStackableList<FilterScope<T>> Scopes { get; }

        public IStackableList<IType> Types { get; } = new StackableList<IType>();

        public IStackableList<IInputField> Operations { get; } = new StackableList<IInputField>();

        public IList<IError> Errors { get; } = new List<IError>();

        public virtual FilterScope<T> CreateScope()
        {
            return new FilterScope<T>();
        }
    }
}