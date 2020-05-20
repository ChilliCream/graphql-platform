using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace HotChocolate.Types.Filters
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
        }

        public ParameterExpression Parameter { get; }

        public bool InMemory { get; }
    }
}
