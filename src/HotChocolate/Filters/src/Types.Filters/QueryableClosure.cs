using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using HotChocolate.Types.Filters.Expressions;

namespace HotChocolate.Types.Filters
{
    public class QueryableClosure
    {
        private readonly bool _inMemory;

        public QueryableClosure(
            Type type,
            string parameterName,
            bool inMemory)
        {
            Parameter = Expression.Parameter(type, parameterName);
            Level = new Stack<Queue<Expression>>();
            Instance = new Stack<Expression>();

            Level.Push(new Queue<Expression>());
            Instance.Push(Parameter);
            _inMemory = inMemory;
        }

        public ParameterExpression Parameter { get; }

        public Stack<Queue<Expression>> Level { get; }

        public Stack<Expression> Instance { get; }


        public LambdaExpression CreateLambda()
        {
            if (_inMemory)
            {
                return Expression.Lambda(GetExpressionBodyWithNullCheck(), Parameter);

            }
            return Expression.Lambda(Level.Peek().Peek(), Parameter);
        }

        public Expression<T> CreateLambda<T>()
        {
            if (_inMemory)
            {
                return Expression.Lambda<T>(GetExpressionBodyWithNullCheck(), Parameter);
            }
            return Expression.Lambda<T>(Level.Peek().Peek(), Parameter);
        }

        private Expression GetExpressionBodyWithNullCheck()
            => FilterExpressionBuilder.NotNullAndAlso(Parameter, Level.Peek().Peek());
    }
}
