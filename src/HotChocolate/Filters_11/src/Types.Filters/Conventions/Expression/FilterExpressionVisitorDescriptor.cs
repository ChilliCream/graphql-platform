using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Types.Filters.Conventions
{
    public delegate bool FilterOperationHandler(
        FilterOperation operation,
        IInputType type,
        IValueNode value,
        IQueryableFilterVisitorContext context,
        [NotNullWhen(true)]out Expression? result);

    public delegate bool FilterFieldEnter(
        FilterOperationField field,
        ObjectFieldNode node,
        IQueryableFilterVisitorContext context,
        [NotNullWhen(true)]out ISyntaxVisitorAction? action);

    public delegate void FilterFieldLeave(
        FilterOperationField field,
        ObjectFieldNode node,
        IQueryableFilterVisitorContext context);

    public class FilterExpressionVisitorDescriptor :
        FilterVisitorDescriptorBase<FilterExpressionVisitorDefinition>,
        IFilterExpressionVisitorDescriptor
    {
        private readonly ConcurrentDictionary<FilterKind, FilterExpressionTypeDescriptor> _types =
            new ConcurrentDictionary<FilterKind, FilterExpressionTypeDescriptor>();

        private readonly IFilterConventionDescriptor _convention;

        protected FilterExpressionVisitorDescriptor(
            IFilterConventionDescriptor convention)
        {
            _convention = convention;
        }

        protected override FilterExpressionVisitorDefinition Definition { get; } =
            new FilterExpressionVisitorDefinition();

        public IFilterConventionDescriptor And() => _convention;

        public override FilterExpressionVisitorDefinition CreateDefinition()
        {
            var fieldHandler =
                new Dictionary<FilterKind, (FilterFieldEnter? enter, FilterFieldLeave? leave)>();
            var operationHandler =
                new Dictionary<(FilterKind, FilterOperationKind), FilterOperationHandler>();

            foreach (FilterExpressionTypeDescriptor typeDescriptor in _types.Values)
            {
                FilterExpressionTypeDefinition definition = typeDescriptor.CreateDefinition();
                if (definition.Enter != null || definition.Leave != null)
                {
                    fieldHandler[definition.FilterKind] = (definition.Enter, definition.Leave);
                }
                foreach (KeyValuePair<FilterOperationKind, FilterOperationHandler> handlerPair in
                    definition.OperationHandlers)
                {
                    operationHandler[(definition.FilterKind, handlerPair.Key)] = handlerPair.Value;
                }
            }
            Definition.FieldHandler = fieldHandler;
            Definition.OperationHandler = operationHandler;
            return Definition;
        }

        public IFilterExpressionTypeDescriptor Kind(FilterKind kind) =>
            _types.GetOrAdd(kind, _ => FilterExpressionTypeDescriptor.New(this, kind));

        public static FilterExpressionVisitorDescriptor New(
            IFilterConventionDescriptor convention) =>
            new FilterExpressionVisitorDescriptor(convention);
    }
}
