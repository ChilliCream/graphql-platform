using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types;

namespace HotChocolate.Data.Filters
{
    public class FilterVisitor<T, TContext>
        : FilterVisitorBase<T, TContext>
        where TContext : FilterVisitorContext<T>
    {
        public FilterOperationCombinator<T, TContext> Combinator { get; internal set; } = null!;

        protected override ISyntaxVisitorAction OnFieldEnter(
            TContext context,
            IFilterInputType declaringType,
            IFilterField field,
            IType fieldType,
            ObjectFieldNode node)
        {
            if (field?.Handler is FilterFieldHandler<T, TContext> handler &&
                handler.TryHandleEnter(
                    context,
                    declaringType,
                    field,
                    fieldType,
                    node,
                    out ISyntaxVisitorAction? action))
            {
                return action;
            }
            return SyntaxVisitor.SkipAndLeave;
        }

        protected override ISyntaxVisitorAction OnFieldLeave(
            TContext context,
            IFilterInputType declaringType,
            IFilterField field,
            IType fieldType,
            ObjectFieldNode node)
        {
            if (field?.Handler is FilterFieldHandler<T, TContext> handler &&
                handler.TryHandleLeave(
                    context,
                    declaringType,
                    field,
                    fieldType,
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
            [NotNullWhen(true)] out T combined)
        {
            if (Combinator is { })
            {
                return Combinator.TryCombineOperations(
                   context, operations, combinator, out combined);
            }
            combined = default;
            return false;
        }
    }
}