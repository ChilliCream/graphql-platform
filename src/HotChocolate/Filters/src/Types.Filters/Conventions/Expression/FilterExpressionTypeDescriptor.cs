using System;
using System.Collections.Concurrent;
using System.Linq;

namespace HotChocolate.Types.Filters.Conventions
{
    public class FilterExpressionTypeDescriptor
        : IFilterExpressionTypeDescriptor
    {
        private readonly FilterExpressionVisitorDescriptor _descriptor;

        protected FilterExpressionTypeDescriptor(
            FilterExpressionVisitorDescriptor descriptor,
            FilterKind kind)
        {
            _descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
            Definition.FilterKind = kind;
        }

        private readonly Dictionary<FilterOperationKind,
            FilterExpressionOperationDescriptor> _operations = 
            new Dictionary<FilterOperationKind, FilterExpressionOperationDescriptor>();

        protected FilterExpressionTypeDefinition Definition { get; } =
            new FilterExpressionTypeDefinition();

        public IFilterExpressionVisitorDescriptor And() => _descriptor;

        public IFilterExpressionTypeDescriptor Enter(FilterFieldEnter handler)
        {
            Definition.Enter = handler;
            return this;
        }

        public IFilterExpressionTypeDescriptor Leave(FilterFieldLeave handler)
        {
            Definition.Leave = handler;
            return this;
        }

        public IFilterExpressionOperationDescriptor Operation(FilterOperationKind kind) =>
            _operations.GetOrAdd(kind, _ => FilterExpressionOperationDescriptor.New(this, kind));

        public FilterExpressionTypeDefinition CreateDefinition()
        {
            Definition.OperationHandlers = _operations.Values
                .Select(x => x.CreateDefinition())
                .Where(x => x.Handler != null)
                .ToDictionary(x => x.OperationKind, x => x.Handler!);

            return Definition;
        }

        public static FilterExpressionTypeDescriptor New(
            FilterExpressionVisitorDescriptor descriptor,
            FilterKind kind) =>
            new FilterExpressionTypeDescriptor(descriptor, kind);
    }
}
