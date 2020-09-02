using System.Linq.Expressions;
using HotChocolate.Internal;

namespace HotChocolate.Data.Filters.Expressions
{
    public class QueryableScope
        : FilterScope<Expression>
    {
        public QueryableScope(
            IExtendedType type,
            string parameterName,
            bool inMemory)
        {
            Parameter = Expression.Parameter(type.Source, parameterName);
            InMemory = inMemory;
            Instance.Push(Parameter);
        }

        public ParameterExpression Parameter { get; }

        public bool InMemory { get; }
    }
}
