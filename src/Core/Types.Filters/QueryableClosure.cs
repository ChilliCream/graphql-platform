using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using HotChocolate.Types.Filters.Expressions;

namespace HotChocolate.Types.Filters
{
    public class QueryableClosure
    {
        public QueryableClosure(Type type, string parameterName)
        {
            Parameter = Expression.Parameter(type, parameterName);
            Level = new Stack<Queue<Expression>>();
            Instance = new Stack<Expression>();

            Level.Push(new Queue<Expression>());
            Instance.Push(Parameter);
        }

        public ParameterExpression Parameter { get; }
        public Stack<Queue<Expression>> Level { get; }

        public Stack<Expression> Instance { get; }


        public LambdaExpression CreateLambda()
        {
            return Expression.Lambda(GetSafeExpressionBody(), Parameter);
        }

        public Expression<T> CreateLambda<T>()
        {
            return Expression.Lambda<T>(GetSafeExpressionBody(), Parameter);
        }

        private Expression GetSafeExpressionBody()
        {
            return FilterExpressionBuilder.NotNullAndAlso(Parameter, Level.Peek().Peek());
        }
    }
}
