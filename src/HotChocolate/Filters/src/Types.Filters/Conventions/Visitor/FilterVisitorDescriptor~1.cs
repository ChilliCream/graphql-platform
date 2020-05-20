using System.Collections.Generic;
using HotChocolate.Types.Filters.Extensions;

namespace HotChocolate.Types.Filters.Conventions
{
    public class FilterVisitorDescriptor<T>
        : FilterVisitorDescriptorBase, IFilterVisitorDescriptor<T>
    {
        private readonly Dictionary<FilterKind, FilterVisitorTypeDescriptor<T>> _types =
            new Dictionary<FilterKind, FilterVisitorTypeDescriptor<T>>();

        private readonly Dictionary<FilterCombinator, FilterVisitorCombinatorDescriptor<T>> _combinators =
            new Dictionary<FilterCombinator, FilterVisitorCombinatorDescriptor<T>>();

        private readonly IFilterConventionDescriptor _convention;

        protected FilterVisitorDescriptor(
            IFilterConventionDescriptor convention)
        {
            _convention = convention;
        }

        protected FilterVisitorDefinition<T> Definition { get; } =
            new FilterVisitorDefinition<T>();

        public IFilterConventionDescriptor And() => _convention;

        public override FilterVisitorDefinitionBase CreateDefinition()
        {
            var fieldHandler =
                new Dictionary<FilterKind, (FilterFieldEnter<T>? enter, FilterFieldLeave<T>? leave)>();
            var operationHandler =
                new Dictionary<(FilterKind, FilterOperationKind), FilterOperationHandler<T>>();
            var combinator =
                new Dictionary<FilterCombinator, FilterOperationCombinator<T>>();

            foreach (FilterVisitorTypeDescriptor<T> typeDescriptor in _types.Values)
            {
                FilterVisitorTypeDefinition<T> definition = typeDescriptor.CreateDefinition();
                if (definition.Enter != null || definition.Leave != null)
                {
                    fieldHandler[definition.FilterKind] = (definition.Enter, definition.Leave);
                }
                foreach (KeyValuePair<FilterOperationKind, FilterOperationHandler<T>> handlerPair in
                    definition.OperationHandlers)
                {
                    operationHandler[(definition.FilterKind, handlerPair.Key)] = handlerPair.Value;
                }
            }

            foreach (FilterVisitorCombinatorDescriptor<T> combinatorDesc in _combinators.Values)
            {
                FilterVisitorCombinatorDefinition<T> definition = combinatorDesc.CreateDefinition();
                if (definition.Handler != null)
                {
                    combinator[definition.Combinator] = definition.Handler;
                }
            }
            Definition.FieldHandler = fieldHandler;
            Definition.OperationHandler = operationHandler;
            Definition.OperationCombinator = combinator;
            return Definition;
        }

        public IFilterVisitorTypeDescriptor<T> Kind(FilterKind kind) =>
            _types.GetOrAdd(kind, _ => FilterVisitorTypeDescriptor<T>.New(this, kind));

        public static FilterVisitorDescriptor<T> New(
            IFilterConventionDescriptor convention) =>
            new FilterVisitorDescriptor<T>(convention);

        public IFilterCombinatorDescriptor<T> Combinator(FilterCombinator combinator) =>
            _combinators.GetOrAdd(combinator,
                _ => FilterVisitorCombinatorDescriptor<T>.New(this, combinator));

        public IFilterVisitorDescriptor<T> Middleware<TMiddleware>()
            where TMiddleware : class, IFilterMiddleware<T>, new()
        {
            Definition.FilterMiddleware = new TMiddleware();
            return this;
        }

        public IFilterVisitorDescriptor<T> Middleware(IFilterMiddleware<T> middleware)
        {
            Definition.FilterMiddleware = middleware;
            return this;
        }
    }
}
