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

        protected abstract ISyntaxVisitorAction OnFieldEnter(
            TContext context,
            IFilterInputType declaringType,
            IFilterField field,
            IType fieldType,
            ObjectFieldNode node);

        protected abstract ISyntaxVisitorAction OnFieldLeave(
            TContext context,
            IFilterInputType declaringType,
            IFilterField field,
            IType fieldType,
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

            context.Types.TryPeekAt(1, out IType? delaringType);
            IInputField? currentOperation = context.Operations.Peek();
            IType? fieldType = context.Types.Peek();
            if (currentOperation is FilterField field &&
                delaringType is IFilterInputType declaringFilterType &&
                fieldType is { })
            {
                return OnFieldEnter(context, declaringFilterType, field, fieldType, node);
            }

            return Continue;
        }

        protected override ISyntaxVisitorAction Leave(
            ObjectFieldNode node,
            TContext context)
        {
            ISyntaxVisitorAction? result = Continue;

            context.Types.TryPeekAt(1, out IType? delaringType);
            IInputField? currentOperation = context.Operations.Peek();
            IType? fieldType = context.Types.Peek();
            if (currentOperation is FilterField field &&
                delaringType is IFilterInputType declaringFilterType &&
                fieldType is { })
            {
                result = OnFieldLeave(context, declaringFilterType, field, fieldType, node);
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