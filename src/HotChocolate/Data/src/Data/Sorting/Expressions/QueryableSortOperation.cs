using System;
using System.Linq.Expressions;

namespace HotChocolate.Data.Sorting.Expressions
{
    public abstract class QueryableSortOperation
    {
        protected QueryableSortOperation(
            Expression selector,
            ParameterExpression parameterExpression)
        {
            Selector = selector ?? throw new ArgumentNullException(nameof(selector));
            ParameterExpression = parameterExpression ??
                throw new ArgumentNullException(nameof(parameterExpression));
        }

        protected QueryableSortOperation(QueryableFieldSelector fieldSelector)
        {
            Selector = fieldSelector.Selector;
            ParameterExpression = fieldSelector.ParameterExpression;
        }

        public Expression Selector { get; }

        public ParameterExpression ParameterExpression { get; }

        public abstract Expression CompileOrderBy(Expression expression);

        public abstract Expression CompileThenBy(Expression expression);
    }
}
