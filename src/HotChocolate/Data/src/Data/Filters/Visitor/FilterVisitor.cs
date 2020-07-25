using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Data.Filters;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types;
using System;

namespace HotChocolate.Data.Filters
{

    public abstract class FilterVisitor<T, TContext>
        : FilterVisitorBase<T, TContext>
        where TContext : FilterVisitorContext<T>
    {
        public FilterOperationCombinator Combinator { get; internal set; } = null!;

        protected override ISyntaxVisitorAction OnAfterFieldEnter(
            IFilterInputType type,
            IFilterField field,
            ObjectValueNode node,
            TContext context)
        {
            if (field?.Handler is FilterFieldHandler<T, TContext> handler &&
                handler.TryHandleEnter(context, type, field, node, out ISyntaxVisitorAction? action))
            {
                return action;
            }
            return SyntaxVisitor.Skip;
        }

        protected override ISyntaxVisitorAction OnBeforeFieldLeave(
            IFilterInputType type,
            IFilterField field,
            ObjectValueNode node,
            TContext context)
        {
            if (field?.Handler is FilterFieldHandler<T, TContext> handler &&
                handler.TryHandleLeave(context, type, field, node, out ISyntaxVisitorAction? action))
            {
                return action;
            }
            return SyntaxVisitor.Skip;
        }

        protected override ISyntaxVisitorAction OnOperationEnter(
            IFilterInputType type,
            IFilterOperationField field,
            ObjectFieldNode node,
            TContext context)
        {
            if (field?.Handler is FilterFieldHandler<T, TContext> handler &&
                handler.TryHandleOperation(
                    context, type, field, node.Value, out T result))
            {
                context.GetLevel().Enqueue(result);
                return SkipAndLeave;
            }
            return SyntaxVisitor.Skip;
        }

        protected override bool TryCombineOperations(
            IEnumerable<T> operations,
            FilterCombinator combinator,
            [NotNullWhen(true)] out T combined)
        {
            if (Combinator is { })
            {
                return Combinator.TryCombineOperations(operations, combinator, out combined);
            }
            combined = default;
            return false;
        }
    }
}