using System;
using System.Linq.Expressions;

namespace HotChocolate.Data.Filters.Expressions
{
    public class QueryableScope
        : FilterScope<Expression>
    {
        public QueryableScope(
            Type type,
            string parameterName,
            bool inMemory)
        {
            Parameter = Expression.Parameter(type, parameterName);
            InMemory = inMemory;
            Instance.Push(Parameter);
        }

        public ParameterExpression Parameter { get; }

        public bool InMemory { get; }
    }
}
