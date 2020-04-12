using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types.Filters.Expressions;

namespace HotChocolate.Types.Filters.Conventions
{
    public delegate Expression FilterOperationHandler(
            FilterOperation operation,
            IInputType type,
            IValueNode value,
            IQueryableFilterVisitorContext context);

    public delegate bool FilterFieldEnter(
            FilterOperationField field,
            ObjectFieldNode node,
            IQueryableFilterVisitorContext context,
            out ISyntaxVisitorAction action);

    public delegate void FilterFieldLeave(
            FilterOperationField field,
            ObjectFieldNode node,
            IQueryableFilterVisitorContext context);

    public class FilterExpressionVisitorDescriptor :
        FilterVisitorDescriptorBase<FilterExpressionVisitorDefintion>,
        IFilterExpressionVisitorDescriptor
    {
        private readonly ConcurrentDictionary<FilterKind, FilterExpressionTypeDescriptor> _types
            = new ConcurrentDictionary<FilterKind, FilterExpressionTypeDescriptor>();

        private readonly IFilterConventionDescriptor _convention;

        protected FilterExpressionVisitorDescriptor(
            IFilterConventionDescriptor convention)
        {
            _convention = convention;
        }

        protected override FilterExpressionVisitorDefintion Definition { get; }
            = new FilterExpressionVisitorDefintion();

        public IFilterConventionDescriptor And() => _convention;

        public override FilterExpressionVisitorDefintion CreateDefinition()
        {
            var fieldHandler
                = new Dictionary<FilterKind, (FilterFieldEnter? enter, FilterFieldLeave? leave)>();
            var operationHandler
                = new Dictionary<(FilterKind, FilterOperationKind), FilterOperationHandler>();

            foreach (FilterExpressionTypeDescriptor typeDescriptor in _types.Values)
            {
                FilterExpressionTypeDefinition definition = typeDescriptor.CreateDefinition();
                if (definition.Enter != null || definition.Leave != null)
                {
                    fieldHandler[definition.FilterKind] = (definition.Enter, definition.Leave);
                }
                foreach ((FilterOperationKind operationKind, FilterOperationHandler handler) in
                    definition.OperationHandlers)
                {
                    operationHandler[(definition.FilterKind, operationKind)] = handler;
                }
            }
            Definition.FieldHandler = fieldHandler;
            Definition.OperationHandler = operationHandler;
            return Definition;
        }

        public IFilterExpressionTypeDescriptor Type(FilterKind kind)
            => _types.GetOrAdd(
                kind,
                _ => FilterExpressionTypeDescriptor.New(this, kind));

        public static FilterExpressionVisitorDescriptor New(IFilterConventionDescriptor convention)
            => new FilterExpressionVisitorDescriptor(convention);
    }
}
