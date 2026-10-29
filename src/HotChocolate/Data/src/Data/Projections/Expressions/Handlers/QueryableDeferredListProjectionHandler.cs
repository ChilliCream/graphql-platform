using System.Diagnostics.CodeAnalysis;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Filters.Expressions;
using HotChocolate.Data.Sorting;
using HotChocolate.Execution.Internal;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Data.Projections.Expressions.Handlers;

internal sealed class QueryableDeferredListProjectionHandler(QueryableProjectionListHandler handler)
    : ProjectionFieldHandler<QueryableProjectionContext>
{
    public override bool CanHandle(Selection selection) => handler.CanHandle(selection);

    public override QueryableProjectionContext OnBeforeEnter(
        QueryableProjectionContext context,
        Selection selection)
        => handler.OnBeforeEnter(context, selection);

    public override bool TryHandleEnter(
        QueryableProjectionContext context,
        Selection selection,
        [NotNullWhen(true)] out ISelectionVisitorAction? action)
        => handler.TryHandleEnter(context, selection, out action);

    public override QueryableProjectionContext OnAfterEnter(
        QueryableProjectionContext context,
        Selection selection,
        ISelectionVisitorAction action)
        => handler.OnAfterEnter(context, selection, action);

    public override bool TryHandleLeave(
        QueryableProjectionContext context,
        Selection selection,
        [NotNullWhen(true)] out ISelectionVisitorAction? action)
        => handler.TryHandleLeave(context, selection, out action);

    public override QueryableProjectionContext OnAfterLeave(
        QueryableProjectionContext context,
        Selection selection,
        ISelectionVisitorAction action)
        => handler.OnAfterLeave(context, selection, action);

    public override QueryableProjectionContext OnBeforeLeave(
        QueryableProjectionContext context,
        Selection selection)
    {
        if (context.TryGetQueryableScope(out var scope)
            && selection.Arguments.TryCoerceArguments(context.ResolverContext, out var arguments))
        {
            if (selection.GetFilterFeature() is { } filter
                && arguments.TryGetValue(filter.ArgumentName, out var argument)
                && argument.Type is IFilterInputType filterType
                && argument.ValueLiteral is { } value and not NullValueNode)
            {
                var filterContext = filter.ArgumentVisitor(value, filterType, false);

                if (filterContext.TryCreateLambda(out var predicate))
                {
                    ProjectionDependencyCollector.Collect(
                        scope,
                        context.NullabilityInfoContext,
                        predicate.Body,
                        predicate.Parameters[0]);
                }
            }

            if (selection.Field.Features.TryGet(out SortingFeature? sort)
                && arguments.TryGetValue(sort.ArgumentName, out argument)
                && argument.Type.NamedType() is ISortInputType sortType
                && argument.ValueLiteral is { } order and not NullValueNode)
            {
                var sortContext = sort.ArgumentVisitor(order, sortType, false);

                foreach (var operation in sortContext.Operations)
                {
                    ProjectionDependencyCollector.Collect(
                        scope,
                        context.NullabilityInfoContext,
                        operation.Selector,
                        operation.ParameterExpression);
                }
            }
        }

        return handler.OnBeforeLeave(context, selection);
    }
}
