using System;
using System.Linq.Expressions;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types.Filters.Conventions;

#nullable enable
namespace HotChocolate.Types.Filters
{
    public class ExpressionVisitorMock
    {
        private int filterOperationCallCounter = 0;
        private FilterOperationHandler? filterOperationHandler;

        private int fieldEnterCallCounter = 0;
        private FilterFieldEnter? fieldEnterHandler;

        private int fieldLeaveCallCounter = 0;
        private FilterFieldLeave? fieldLeaveHandler;

        public ExpressionVisitorMock Setup(FilterOperationHandler handler)
        {
            filterOperationCallCounter = 0;
            filterOperationHandler = handler;
            return this;
        }

        public ExpressionVisitorMock Setup(FilterFieldEnter handler)
        {
            fieldEnterCallCounter = 0;
            fieldEnterHandler = handler;
            return this;
        }

        public ExpressionVisitorMock Setup(FilterFieldLeave handler)
        {
            fieldLeaveCallCounter = 0;
            fieldLeaveHandler = handler;
            return this;
        }

        public int CallCount(FilterOperationHandler _)
            => filterOperationCallCounter;

        public int CallCount(FilterFieldEnter _)
            => fieldEnterCallCounter;

        public int CallCount(FilterFieldLeave _)
            => fieldLeaveCallCounter;

        public Expression FilterOperationHandler(
                FilterOperation operation,
                IInputType type,
                IValueNode value,
                IQueryableFilterVisitorContext context)
        {
            if (filterOperationHandler == null)
            {
                throw new InvalidOperationException();
            }

            filterOperationCallCounter++;
            return filterOperationHandler(operation, type, value, context);
        }

        public bool FilterFieldEnter(
                FilterOperationField field,
                ObjectFieldNode node,
                IQueryableFilterVisitorContext context,
                out ISyntaxVisitorAction action)
        {
            if (fieldEnterHandler == null)
            {
                throw new InvalidOperationException();
            }

            fieldEnterCallCounter++;
            return fieldEnterHandler(field, node, context, out action);
        }

        public void FilterFieldLeave(
                FilterOperationField field,
                ObjectFieldNode node,
                IQueryableFilterVisitorContext context)
        {
            if (fieldLeaveHandler == null)
            {
                throw new InvalidOperationException();
            }

            fieldLeaveCallCounter++;
            fieldLeaveHandler(field, node, context);
        }

        public static ExpressionVisitorMock Create(FilterOperationHandler handler)
            => new ExpressionVisitorMock().Setup(handler);

        public static ExpressionVisitorMock Create(FilterFieldEnter handler)
            => new ExpressionVisitorMock().Setup(handler);

        public static ExpressionVisitorMock Create(FilterFieldLeave handler)
            => new ExpressionVisitorMock().Setup(handler);
    }
}
