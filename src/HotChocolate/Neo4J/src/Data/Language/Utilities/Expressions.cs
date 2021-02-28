using System;
using System.Collections.Generic;

namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// Utility methods for dealing with expressions.
    /// </summary>
    public static class Expressions
    {
        //private Expressions() { }
        public static Expression NameOrExpression<T>(T expression) where T : Expression
        {
            if (expression is Named named)
            {
                return named.GetSymbolicName();
            }
            return expression;
        }

        public static SymbolicName[] CreateSymbolicNames(INamed[] variables) =>
            Array.ConvertAll(variables, i => i.GetSymbolicName());

        public static SymbolicName[] CreateSymbolicNames(string[] variables) =>
            Array.ConvertAll(variables, SymbolicName.Of);

    }
}
