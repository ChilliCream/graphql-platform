using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Resolvers;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Filters.Conventions
{
    public delegate bool FilterOperationHandler<T>(
        FilterOperation operation,
        IInputType type,
        IValueNode value,
        IFilterVisitorContext<T> context,
        [MaybeNullWhen(true)] out T result);

    public delegate bool FilterFieldEnter<T>(
        FilterOperationField field,
        ObjectFieldNode node,
        IFilterVisitorContext<T> context,
        [NotNullWhen(true)] out ISyntaxVisitorAction? action);

    public delegate void FilterFieldLeave<T>(
        FilterOperationField field,
        ObjectFieldNode node,
        IFilterVisitorContext<T> context);

    public delegate T FilterOperationCombinator<T>(IReadOnlyList<T> operations);

    public class FilterVisitorDefinition<T> :
        FilterVisitorDefinitionBase
    {
        public IReadOnlyDictionary<FilterKind, (FilterFieldEnter<T>? enter, FilterFieldLeave<T>? leave)> FieldHandler
        { get; set; } = ImmutableDictionary<FilterKind, (FilterFieldEnter<T>? enter, FilterFieldLeave<T>? leave)>.Empty;

        public IReadOnlyDictionary<(FilterKind, FilterOperationKind), FilterOperationHandler<T>> OperationHandler
        { get; set; } = ImmutableDictionary<(FilterKind, FilterOperationKind), FilterOperationHandler<T>>.Empty;

        public IReadOnlyDictionary<FilterCombinator, FilterOperationCombinator<T>> OperationCombinator
        { get; set; } = ImmutableDictionary<FilterCombinator, FilterOperationCombinator<T>>.Empty;

        public IFilterMiddleware<T> FilterMiddleware = default!;

        public override Task ApplyFilter<TSource>(
            IFilterConvention convention,
            FieldDelegate next,
            ITypeConversion converter,
            IMiddlewareContext context)
        {
            string argumentName = convention!.GetArgumentName();
            if (context.Field.Arguments[argumentName].Type is InputObjectType iot &&
                iot is IFilterInputType fit)
            {
                return FilterMiddleware.ApplyFilter<TSource>(
                    this, context, next, convention, converter, fit, iot);
            }
            return next(context);
        }
    }
}
