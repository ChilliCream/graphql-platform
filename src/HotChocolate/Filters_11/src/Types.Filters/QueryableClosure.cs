using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace HotChocolate.Types.Filters
{
    public class QueryableClosure
    {
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
            InMemory = inMemory;
        }

        public ParameterExpression Parameter { get; }

        public Stack<Queue<Expression>> Level { get; }

        public Stack<Expression> Instance { get; }

        public bool InMemory { get; }
    }
}
