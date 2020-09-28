using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Data.Filters
{
    public abstract class FilterVisitorBase<TContext, T>
        : FilterVisitorBase<TContext>
        where TContext : FilterVisitorContext<T>
    {
        protected FilterVisitorBase()
        {
        }

        protected abstract ISyntaxVisitorAction OnFieldEnter(
            TContext context,
            IFilterField field,
            ObjectFieldNode node);

        protected abstract ISyntaxVisitorAction OnFieldLeave(
            TContext context,
            IFilterField field,
            ObjectFieldNode node);

        protected abstract bool TryCombineOperations(
            TContext context,
            Queue<T> operations,
            FilterCombinator combinator,
            [NotNullWhen(true)] out T combined);

        protected override ISyntaxVisitorAction Leave(
            ObjectValueNode node,
            TContext context)
        {
            Queue<T> operations = context.PopLevel();

            if (TryCombineOperations(context,
                    operations,
                    FilterCombinator.And,
                    out T combined))
            {
                context.GetLevel().Enqueue(combined);
            }

            return Continue;
        }
        protected override ISyntaxVisitorAction Enter(
            ObjectValueNode node,
            TContext context)
        {
            context.PushLevel(new Queue<T>());

            return Continue;
        }

        protected override ISyntaxVisitorAction Enter(
            ObjectFieldNode node,
            TContext context)
        {
            base.Enter(node, context);

            if (context.Operations.Peek() is IFilterField field)
            {
                return OnFieldEnter(context, field, node);
            }

            return Continue;
        }

        protected override ISyntaxVisitorAction Leave(
            ObjectFieldNode node,
            TContext context)
        {
            ISyntaxVisitorAction? result = Continue;

            if (context.Operations.Peek() is IFilterField field)
            {
                result = OnFieldLeave(context, field, node);
            }

            base.Leave(node, context);

            return result;
        }

        protected override ISyntaxVisitorAction Enter(
            ListValueNode node,
            TContext context)
        {
            context.PushLevel(new Queue<T>());
            return Continue;
        }

        protected override ISyntaxVisitorAction Leave(
            ListValueNode node,
            TContext context)
        {
            FilterCombinator combinator =
                context.Operations.Peek() is OrField
                    ? FilterCombinator.Or
                    : FilterCombinator.And;

            Queue<T> operations = context.PopLevel();

            if (TryCombineOperations(
                    context,
                    operations,
                combinator,
                out T combined))
            {
                context.GetLevel().Enqueue(combined);
            }

            return Continue;
        }
    }
}
