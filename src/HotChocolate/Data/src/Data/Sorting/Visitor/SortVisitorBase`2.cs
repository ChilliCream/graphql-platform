using System;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Data.Sorting
{
    public abstract class SortVisitorBase<TContext, T>
        : SortVisitorBase<TContext>
        where TContext : SortVisitorContext<T>
    {
        protected SortVisitorBase()
        {
        }

        protected abstract ISyntaxVisitorAction OnFieldEnter(
            TContext context,
            ISortField field,
            ObjectFieldNode node);

        protected abstract ISyntaxVisitorAction OnFieldLeave(
            TContext context,
            ISortField field,
            ObjectFieldNode node);

        protected abstract ISyntaxVisitorAction OnOperationEnter(
            TContext context,
            ISortField field,
            ISortEnumValue? sortEnumValue,
            EnumValueNode enumValueNode);

        protected override ISyntaxVisitorAction Enter(
            ObjectFieldNode node,
            TContext context)
        {
            base.Enter(node, context);

            if (context.Fields.Peek() is ISortField field)
            {
                return OnFieldEnter(context, field, node);
            }

            return Continue;
        }

        protected override ISyntaxVisitorAction Enter(IValueNode node, TContext context)
        {
            base.Enter(node, context);

            if (node is not NullValueNode &&
                context.Fields.Peek() is ISortField field &&
                field.Type is SortEnumType sortType &&
                node is EnumValueNode enumValueNode)
            {
                return OnOperationEnter(
                    context,
                    field,
                    sortType.ParseSortLiteral(node),
                    enumValueNode);
            }

            return Continue;
        }

        protected override ISyntaxVisitorAction Leave(
            ObjectFieldNode node,
            TContext context)
        {
            ISyntaxVisitorAction result = Continue;

            if (context.Fields.Peek() is ISortField field)
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
            if (context.Fields.Count > 0 &&
                context.Fields.Peek() is ISortField sortField)
            {
                context.ReportError(ErrorHelper.SortingVisitor_ListValues(sortField, node));
            }
            else
            {
                return Continue;
            }

            return Break;
        }

        protected override ISyntaxVisitorAction Leave(
            ListValueNode node,
            TContext context)
        {
            if (context.Fields.Count > 0 &&
                context.Fields.Peek() is ISortField sortField)
            {
                context.ReportError(ErrorHelper.SortingVisitor_ListValues(sortField, node));
            }
            else
            {
                return Continue;
            }

            return Break;
        }
    }
}
