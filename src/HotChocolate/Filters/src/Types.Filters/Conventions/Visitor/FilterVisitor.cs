using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types.Filters.Conventions;

namespace HotChocolate.Types.Filters
{
    public class FilterVisitor<T>
        : FilterVisitorBase<FilterVisitorContext<T>>
    {
        protected FilterVisitor()
        {
        }

        protected override ISyntaxVisitorAction Enter(
            ObjectValueNode node,
            FilterVisitorContext<T> context)
        {
            context.PushLevel(new Queue<T>());
            return Continue;
        }

        protected override ISyntaxVisitorAction Leave(
            ObjectValueNode node,
            FilterVisitorContext<T> context)
        {
            Queue<T> operations = context.PopLevel();

            if (context.TryCombineOperations(
                operations,
                FilterCombinator.AND,
                out T combined))
            {
                context.GetLevel().Enqueue(combined);
            }

            return Continue;
        }

        protected override ISyntaxVisitorAction Enter(
            ObjectFieldNode node,
            FilterVisitorContext<T> context)
        {
            base.Enter(node, context);

            if (context.Operations.Peek() is FilterOperationField field)
            {
                if (context.TryGetEnterHandler(
                    field.Operation.FilterKind, out FilterFieldEnter<T>? enter) &&
                        enter(
                            field,
                            node,
                            context,
                            out ISyntaxVisitorAction? action))
                {
                    return action;
                }

                if (context.TryGetOperation(
                    field.Operation.FilterKind,
                    field.Operation.Kind,
                    out FilterOperationHandler<T>? handler) &&
                    handler(field.Operation, field.Type,
                        node.Value, field, context, out T expression))
                {
                    context.GetLevel().Enqueue(expression);
                }
                return SkipAndLeave;
            }
            return Continue;
        }

        protected override ISyntaxVisitorAction Leave(
            ObjectFieldNode node,
            FilterVisitorContext<T> context)
        {
            if (context.Operations.Peek() is FilterOperationField field)
            {
                if (context.TryGetLeaveHandler(
                    field.Operation.FilterKind, out FilterFieldLeave<T>? leave))
                {
                    leave(field, node, context);
                }
            }
            return base.Leave(node, context);
        }

        protected override ISyntaxVisitorAction Enter(
            ListValueNode node,
            FilterVisitorContext<T> context)
        {
            context.PushLevel(new Queue<T>());
            return Continue;
        }

        protected override ISyntaxVisitorAction Leave(
            ListValueNode node,
            FilterVisitorContext<T> context)
        {
            FilterCombinator combinator =
                context.Operations.Peek() is OrField
                    ? FilterCombinator.OR
                    : FilterCombinator.AND;

            Queue<T> operations = context.PopLevel();

            if (context.TryCombineOperations(
                operations,
                combinator,
                out T combined))
            {
                context.GetLevel().Enqueue(combined);
            }

            return Continue;
        }

        // TODO: DI
        public static FilterVisitor<T> Default = new FilterVisitor<T>();
    }
}
