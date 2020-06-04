using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types.Filters.Conventions;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Filters
{
    public abstract class FilterVisitorContext<T>
        : IFilterVisitorContext<T>
    {
        protected FilterVisitorContext(
            IFilterInputType initialType,
            FilterVisitorDefinition<T> defintion,
            ITypeConversion typeConverter,
            FilterScope<T>? filterScope = null)
        {
            if (initialType is null)
            {
                throw new ArgumentNullException(nameof(initialType));
            }
            Definition = defintion ??
                throw new ArgumentNullException(nameof(defintion));
            TypeConverter = typeConverter ??
                throw new ArgumentNullException(nameof(typeConverter));

            Types.Push(initialType);
            Scopes = new Stack<FilterScope<T>>();
            Scopes.Push(filterScope ?? CreateScope());
        }

        protected FilterVisitorDefinition<T> Definition { get; }

        public ITypeConversion TypeConverter { get; }

        public Stack<FilterScope<T>> Scopes { get; }

        public Stack<IType> Types { get; } = new Stack<IType>();

        public Stack<IInputField> Operations { get; } = new Stack<IInputField>();

        public IList<IError> Errors { get; } = new List<IError>();

        public bool TryGetEnterHandler(
            int kind,
            [NotNullWhen(true)] out FilterFieldEnter<T>? enter)
        {
            if (Definition.FieldHandler.TryGetValue(
                kind, out (FilterFieldEnter<T>? enter, FilterFieldLeave<T>? leave) val) &&
                val.enter is FilterFieldEnter<T>)
            {
                enter = val.enter;
                return true;
            }
            enter = null;
            return false;
        }

        public bool TryGetLeaveHandler(
            int kind,
            [NotNullWhen(true)] out FilterFieldLeave<T>? leave)
        {
            if (Definition.FieldHandler.TryGetValue(
                kind, out (FilterFieldEnter<T>? enter, FilterFieldLeave<T>? leave) val) &&
                val.leave is FilterFieldLeave<T>)
            {
                leave = val.leave;
                return true;
            }
            leave = null;
            return false;
        }

        public bool TryGetOperation(
            int kind,
            int operationKind,
            [NotNullWhen(true)] out FilterOperationHandler<T>? handler)
        {
            if (Definition.OperationHandler.TryGetValue(
                (kind, operationKind), out FilterOperationHandler<T>? operationHandler))
            {
                handler = operationHandler;
                return true;
            }
            handler = null;
            return false;
        }

        public bool TryCombineOperations(
            Queue<T> operations,
            FilterCombinator combinator,
            [NotNullWhen(true)] out T combined)
        {
            if (operations.Count != 0 &&
                Definition.OperationCombinator.TryGetValue(
                    combinator, out FilterOperationCombinator<T>? combine))
            {
                combined = combine(operations, this);
                return true;
            }

            combined = default!;
            return false;
        }

        public virtual FilterScope<T> CreateScope()
        {
            return new FilterScope<T>();
        }
    }
}
