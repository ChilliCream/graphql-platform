using System;

namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// Utility methods for dealing with expressions.
    /// </summary>
    public static class Expressions
    {
        public static Expression NameOrExpression<T>(T expression) where T : Expression
        {
            if (expression is INamed named)
            {
                return named.SymbolicName;
            }

            return expression;
        }

        public static Expression[] CreateSymbolicNames(INamed[] variables) =>
            Array.ConvertAll(variables, i => i.SymbolicName);

        public static SymbolicName[] CreateSymbolicNames(string[] variables) =>
            Array.ConvertAll(variables, SymbolicName.Of);
    }
}
