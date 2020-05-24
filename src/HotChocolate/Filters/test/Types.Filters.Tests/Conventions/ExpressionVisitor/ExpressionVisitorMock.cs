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
        private FilterOperationHandler<Expression>? filterOperationHandler;

        private int fieldEnterCallCounter = 0;
        private FilterFieldEnter<Expression>? fieldEnterHandler;

        private int fieldLeaveCallCounter = 0;
        private FilterFieldLeave<Expression>? fieldLeaveHandler;

        public ExpressionVisitorMock Setup(FilterOperationHandler<Expression> handler)
        {
            filterOperationCallCounter = 0;
            filterOperationHandler = handler;
            return this;
        }

        public ExpressionVisitorMock Setup(FilterFieldEnter<Expression> handler)
        {
            fieldEnterCallCounter = 0;
            fieldEnterHandler = handler;
            return this;
        }

        public ExpressionVisitorMock Setup(FilterFieldLeave<Expression> handler)
        {
            fieldLeaveCallCounter = 0;
            fieldLeaveHandler = handler;
            return this;
        }

        public int CallCount(FilterOperationHandler<Expression> _) => filterOperationCallCounter;

        public int CallCount(FilterFieldEnter<Expression> _) => fieldEnterCallCounter;

        public int CallCount(FilterFieldLeave<Expression> _) => fieldLeaveCallCounter;

        public bool FilterOperationHandler(
                FilterOperation operation,
                IInputType type,
                IValueNode value,
                FilterOperationField field,
                IFilterVisitorContext<Expression> context,
                out Expression? result)
        {
            if (filterOperationHandler == null)
            {
                throw new InvalidOperationException();
            }

            filterOperationCallCounter++;
            return filterOperationHandler(operation, type, value, field, context, out result);
        }

        public bool FilterFieldEnter(
                FilterOperationField field,
                ObjectFieldNode node,
                IFilterVisitorContext<Expression> context,
                out ISyntaxVisitorAction? action)
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
                IFilterVisitorContext<Expression> context)
        {
            if (fieldLeaveHandler == null)
            {
                throw new InvalidOperationException();
            }

            fieldLeaveCallCounter++;
            fieldLeaveHandler(field, node, context);
        }

        public static ExpressionVisitorMock Create(FilterOperationHandler<Expression> handler) =>
            new ExpressionVisitorMock().Setup(handler);

        public static ExpressionVisitorMock Create(FilterFieldEnter<Expression> handler) =>
            new ExpressionVisitorMock().Setup(handler);

        public static ExpressionVisitorMock Create(FilterFieldLeave<Expression> handler) =>
            new ExpressionVisitorMock().Setup(handler);
    }
}
