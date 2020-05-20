using System.Collections.Generic;
using HotChocolate.Types.Filters.Extensions;

namespace HotChocolate.Types.Filters.Conventions
{
    public abstract class FilterVisitorDescriptorBase
    {
        public abstract FilterVisitorDefinitionBase CreateDefinition();
    }

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
            Definition.FieldHandler = fieldHandler;
            Definition.OperationHandler = operationHandler;
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

    public class FilterVisitorCombinatorDescriptor<T> : IFilterCombinatorDescriptor<T>
    {
        private readonly IFilterVisitorDescriptor<T> _descriptor;

        public FilterVisitorCombinatorDescriptor(
            IFilterVisitorDescriptor<T> descriptor,
            FilterCombinator combinator)
        {
            _descriptor = descriptor;
            Definition.Combinator = combinator;
        }

        protected FilterCombinatorDefinition<T> Definition { get; } =
            new FilterCombinatorDefinition<T>();

        public FilterCombinatorDefinition<T> CreateDefinition() => Definition;

        public IFilterVisitorDescriptor<T> And() => _descriptor;

        public IFilterCombinatorDescriptor<T> Handler(
            FilterOperationCombinator<T> operationCombinator)
        {
            Definition.Handler = operationCombinator;
            return this;
        }

        public static FilterVisitorCombinatorDescriptor<T> New(
            IFilterVisitorDescriptor<T> descriptor,
            FilterCombinator combinator) =>
            new FilterVisitorCombinatorDescriptor<T>(descriptor, combinator);
    }

    public class FilterCombinatorDefinition<T>
    {
        public FilterCombinator Combinator { get; set; }

        public FilterOperationCombinator<T>? Handler { get; set; }
    }
}
