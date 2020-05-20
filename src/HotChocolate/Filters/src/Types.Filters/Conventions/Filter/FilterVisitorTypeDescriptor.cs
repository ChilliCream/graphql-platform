using System;
using System.Collections.Concurrent;
using System.Linq;

namespace HotChocolate.Types.Filters.Conventions
{
    public class FilterVisitorTypeDescriptor<T>
        : IFilterVisitorTypeDescriptor<T>
    {
        private readonly FilterVisitorDescriptor<T> _descriptor;

        protected FilterVisitorTypeDescriptor(
            FilterVisitorDescriptor<T> descriptor,
            FilterKind kind)
        {
            _descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
            Definition.FilterKind = kind;
        }

        private readonly ConcurrentDictionary<FilterOperationKind,
            FilterVisitorOperationDescriptor<T>> _operations =
            new ConcurrentDictionary<FilterOperationKind, FilterVisitorOperationDescriptor<T>>();

        protected FilterVisitorTypeDefinition<T> Definition { get; } =
            new FilterVisitorTypeDefinition<T>();

        public IFilterVisitorDescriptor<T> And() => _descriptor;

        public IFilterVisitorTypeDescriptor<T> Enter(FilterFieldEnter<T> handler)
        {
            Definition.Enter = handler;
            return this;
        }

        public IFilterVisitorTypeDescriptor<T> Leave(FilterFieldLeave<T> handler)
        {
            Definition.Leave = handler;
            return this;
        }

        public IFilterVisitorOperationDescriptor<T> Operation(FilterOperationKind kind) =>
            _operations.GetOrAdd(kind, _ => FilterVisitorOperationDescriptor<T>.New(this, kind));

        public FilterVisitorTypeDefinition<T> CreateDefinition()
        {
            Definition.OperationHandlers = _operations.Values
                .Select(x => x.CreateDefinition())
                .Where(x => x.Handler != null)
                .ToDictionary(x => x.OperationKind, x => x.Handler!);

            return Definition;
        }

        public static FilterVisitorTypeDescriptor<T> New(
            FilterVisitorDescriptor<T> descriptor,
            FilterKind kind) =>
            new FilterVisitorTypeDescriptor<T>(descriptor, kind);
    }
}
