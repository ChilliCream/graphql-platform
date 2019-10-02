using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace HotChocolate.Types.Filters
{
    public class QueryableClosure
    {
        public ParameterExpression Parameter { get; }
        public Stack<Queue<Expression>> Level { get; }

        public Stack<Expression> Instance { get; }

        public QueryableClosure(Type type, string parameterName)
        {
            Parameter = Expression.Parameter(type, parameterName);
            Level = new Stack<Queue<Expression>>();
            Instance = new Stack<Expression>();

            Level.Push(new Queue<Expression>());
            Instance.Push(Parameter);
        }

        public LambdaExpression CreateLambda()
        {
            return Expression.Lambda(Level.Peek().Peek(), Parameter);
        }

        public Expression<T> CreateLambda<T>()
        {
            return Expression.Lambda<T>(Level.Peek().Peek(), Parameter);
        }
    }
}
