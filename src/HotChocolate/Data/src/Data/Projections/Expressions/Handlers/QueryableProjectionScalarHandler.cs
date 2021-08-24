using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Execution.Processing;
using HotChocolate.Types;

namespace HotChocolate.Data.Projections.Expressions.Handlers
{
    public class QueryableProjectionScalarHandler
        : QueryableProjectionHandlerBase
    {
        public override bool CanHandle(ISelection selection) =>
            selection.Field.Member is { } &&
            selection.SelectionSet is null;

        public override bool TryHandleEnter(
            QueryableProjectionContext context,
            ISelection selection,
            out ISelectionVisitorAction? action)
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
            ISelection selection,
            [NotNullWhen(true)] out ISelectionVisitorAction? action)
        {
            IObjectField field = selection.Field;

            if (context.Scopes.Count > 0 &&
                context.Scopes.Peek() is QueryableProjectionScope closure &&
                field.Member is PropertyInfo member)
            {
                Expression instance = closure.Instance.Peek();

                closure.Level.Peek()
                    .Enqueue(
                        Expression.Bind(
                            member,
                            Expression.Property(instance, member)));

                action = SelectionVisitor.Continue;
                return true;
            }

            throw new InvalidOperationException();
        }
    }
}
