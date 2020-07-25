using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;

namespace HotChocolate.Data.Filters
{
    public abstract class FilterVisitorBase<T, TContext>
        : FilterVisitorBase<TContext>
        where TContext : FilterVisitorContext<T>
    {
        protected FilterVisitorBase()
        {
        }

        protected abstract ISyntaxVisitorAction OnAfterFieldEnter(
            IFilterInputType type,
            IFilterField field,
            ObjectValueNode node,
            TContext context);

        protected abstract ISyntaxVisitorAction OnBeforeFieldLeave(
            IFilterInputType type,
            IFilterField field,
            ObjectValueNode node,
            TContext context);

        protected abstract ISyntaxVisitorAction OnOperationEnter(
            IFilterInputType type,
            IFilterOperationField field,
            ObjectFieldNode node,
            TContext context);

        protected abstract bool TryCombineOperations(
            IEnumerable<T> operations,
            FilterCombinator combinator,
            [NotNullWhen(true)] out T combined);

        protected override ISyntaxVisitorAction Leave(
            ObjectValueNode node,
            TContext context)
        {
            IInputField? currentOperation = context.Operations.Peek();
            IType? currentType = context.Types.Peek();
            if (currentOperation is FilterField field &&
                currentType is IFilterInputType type)
            {
                return OnAfterFieldEnter(type, field, node, context);
            }

            Queue<T> operations = context.PopLevel();

            if (TryCombineOperations(
                operations,
                FilterCombinator.AND,
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

            IInputField? currentOperation = context.Operations.Peek();
            IType? currentType = context.Types.Peek();
            if (currentOperation is FilterField field &&
                currentType is IFilterInputType type)
            {
                return OnAfterFieldEnter(type, field, node, context);
            }

            return Continue;
        }

        protected override ISyntaxVisitorAction Enter(
            ObjectFieldNode node,
            TContext context)
        {
            base.Enter(node, context);

            IInputField? currentOperation = context.Operations.Peek();
            IType? currentType = context.Types.Peek();
            if (currentOperation is FilterOperationField field &&
                currentType is IFilterInputType type)
            {
                return OnOperationEnter(type, field, node, context);
            }

            return Continue;
        }

        protected override ISyntaxVisitorAction Leave(
            ObjectFieldNode node,
            TContext context)
        {
            return base.Leave(node, context);
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
                    ? FilterCombinator.OR
                    : FilterCombinator.AND;

            Queue<T> operations = context.PopLevel();

            if (TryCombineOperations(
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