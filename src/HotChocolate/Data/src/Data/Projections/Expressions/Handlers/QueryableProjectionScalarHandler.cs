using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Execution.Processing;

namespace HotChocolate.Data.Projections.Expressions.Handlers;

public class QueryableProjectionScalarHandler
    : QueryableProjectionHandlerBase
{
    public override bool CanHandle(Selection selection)
        => selection.Field.Member is not null && selection.IsLeaf;

    public override bool TryHandleEnter(
        QueryableProjectionContext context,
        Selection selection,
        [NotNullWhen(true)] out ISelectionVisitorAction? action)
    {
        if (selection.Field.Member is PropertyInfo { CanWrite: true })
        {
            action = SelectionVisitor.SkipAndLeave;
            return true;
        }

        action = SelectionVisitor.Skip;
        return true;
    }

    public override bool TryHandleLeave(
        QueryableProjectionContext context,
        Selection selection,
        [NotNullWhen(true)] out ISelectionVisitorAction? action)
    {
        var field = selection.Field;

        if (context.Scopes.Count > 0
            && context.Scopes.Peek() is QueryableProjectionScope closure
            && field.Member is PropertyInfo member)
        {
            var instance = closure.Instance.Peek();

            closure.Level.Peek()
                .Enqueue(
                    Expression.Bind(
                        member,
                        Expression.Property(instance, member)));

            action = SelectionVisitor.Continue;
            return true;
        }

        action = SelectionVisitor.Skip;
        return true;
    }

    public static QueryableProjectionScalarHandler Create(ProjectionProviderContext context) => new();
}
