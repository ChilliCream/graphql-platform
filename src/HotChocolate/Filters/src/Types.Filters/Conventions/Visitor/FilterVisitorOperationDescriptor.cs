using System;

namespace HotChocolate.Types.Filters.Conventions
{
    public class FilterVisitorOperationDescriptor<T> : IFilterVisitorOperationDescriptor<T>
    {
        private readonly FilterVisitorTypeDescriptor<T> _descriptor;

        protected FilterVisitorOperationDescriptor(
            FilterVisitorTypeDescriptor<T> descriptor,
            int kind)
        {
            _descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
            Definition.OperationKind = kind;
        }

        protected FilterVisitorOperationDefinition<T> Definition { get; } =
            new FilterVisitorOperationDefinition<T>();

        public IFilterVisitorTypeDescriptor<T> And() => _descriptor;

        public IFilterVisitorOperationDescriptor<T> Handler(FilterOperationHandler<T> handler)
        {
            Definition.Handler = handler;
            return this;
        }

        public FilterVisitorOperationDefinition<T> CreateDefinition() => Definition;

        public static FilterVisitorOperationDescriptor<T> New(
            FilterVisitorTypeDescriptor<T> descriptor,
            int kind) =>
            new FilterVisitorOperationDescriptor<T>(descriptor, kind);
    }
}
