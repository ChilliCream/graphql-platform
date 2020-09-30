using System;
using System.Linq.Expressions;
using HotChocolate.Internal;
using HotChocolate.Types;

namespace HotChocolate.Data.Projections.Expressions
{
    public class QueryableProjectionScope
        : ProjectionScope<Expression>
    {
        public QueryableProjectionScope(
            Type type,
            string parameterName)
        {
            Parameter = Expression.Parameter(type, parameterName);
            Instance.Push(Parameter);
        }

        public ParameterExpression Parameter { get; }
    }
}
