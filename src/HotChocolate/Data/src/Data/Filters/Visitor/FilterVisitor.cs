using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Data.Filters
{
    public class FilterVisitor<TContext, T>
        : FilterVisitorBase<TContext, T>
        where TContext : FilterVisitorContext<T>
    {
        private readonly FilterOperationCombinator<TContext, T> _combinator;

        public FilterVisitor(FilterOperationCombinator<TContext, T> combinator)
        {
            _combinator = combinator;
        }

        protected override ISyntaxVisitorAction OnFieldEnter(
            TContext context,
            IFilterField field,
            ObjectFieldNode node)
        {
            if (field.Handler is IFilterFieldHandler<TContext, T> handler &&
                handler.TryHandleEnter(
                    context,
                    field,
                    node,
                    out ISyntaxVisitorAction? action))
            {
                return action;
            }
            return SyntaxVisitor.SkipAndLeave;
        }

        protected override ISyntaxVisitorAction OnFieldLeave(
            TContext context,
            IFilterField field,
            ObjectFieldNode node)
        {
            if (field?.Handler is IFilterFieldHandler<TContext, T> handler &&
                handler.TryHandleLeave(
                    context,
                    field,
                    node,
                    out ISyntaxVisitorAction? action))
            {
                return action;
            }
            return SyntaxVisitor.Skip;
        }

        protected override bool TryCombineOperations(
            TContext context,
            Queue<T> operations,
            FilterCombinator combinator,
            [NotNullWhen(true)] out T combined) =>
            _combinator.TryCombineOperations(context, operations, combinator, out combined);
    }
    }
