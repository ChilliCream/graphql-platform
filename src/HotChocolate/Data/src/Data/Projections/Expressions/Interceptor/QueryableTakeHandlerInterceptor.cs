using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Data.Projections.Expressions;
using HotChocolate.Data.Projections.Expressions.Handlers;
using HotChocolate.Execution.Processing;
using HotChocolate.Types;

// ReSharper disable once CheckNamespace
namespace HotChocolate.Data.Projections.Handlers;

public abstract class QueryableTakeHandlerInterceptor
    : IProjectionFieldInterceptor<QueryableProjectionContext>
{
    private readonly SelectionFlags _selectionFlags;
    private readonly int _take;

    protected QueryableTakeHandlerInterceptor(SelectionFlags selectionFlags, int take)
    {
        _selectionFlags = selectionFlags;
        _take = take;
    }

    public bool CanHandle(Selection selection) =>
        selection.Field.Member is PropertyInfo { CanWrite: true }
        && selection.IsSelectionFlags(_selectionFlags);

    public void BeforeProjection(
        QueryableProjectionContext context,
        Selection selection)
    {
        if (selection.IsSelectionFlags(_selectionFlags))
        {
            context.PushInstance(
                Expression.Call(
                    typeof(Enumerable),
                    nameof(Enumerable.Take),
                    [selection.Type.ToRuntimeType()],
                    context.PopInstance(),
                    Expression.Constant(_take)));
        }
    }

    public void AfterProjection(
        QueryableProjectionContext context,
        Selection selection)
    {
    }
}
