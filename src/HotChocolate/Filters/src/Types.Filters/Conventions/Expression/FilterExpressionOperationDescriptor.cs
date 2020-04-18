using System;
using HotChocolate.Types.Filters.Expressions;

namespace HotChocolate.Types.Filters.Conventions
{
    public class FilterExpressionOperationDescriptor : IFilterExpressionOperationDescriptor
    {
        private readonly FilterExpressionTypeDescriptor _descriptor;

        protected FilterExpressionOperationDescriptor(
            FilterExpressionTypeDescriptor descriptor,
            FilterOperationKind kind)
        {
            _descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
            Definition.OperationKind = kind;
        }

        protected FilterExpressionOperationDefinition Definition { get; } =
            new FilterExpressionOperationDefinition();

        public IFilterExpressionTypeDescriptor And() => _descriptor;

        public IFilterExpressionOperationDescriptor Handler(FilterOperationHandler handler)
        {
            Definition.Handler = handler;
            return this;
        }

        public FilterExpressionOperationDefinition CreateDefinition() => Definition;

        public static FilterExpressionOperationDescriptor New(
            FilterExpressionTypeDescriptor descriptor,
            FilterOperationKind kind) =>
                new FilterExpressionOperationDescriptor(descriptor, kind);
    }
}
