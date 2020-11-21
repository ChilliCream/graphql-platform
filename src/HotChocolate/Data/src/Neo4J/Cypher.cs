using System;
using System.Collections.Generic;

namespace HotChocolate.Data.Neo4J
{
    /// <summary>
    /// The main entry point into the Cypher DSL.
    /// The Cypher Builder API is intended for framework usage to produce Cypher statements required for database operations.
    /// </summary>
    public class Cypher
    {
        List<Clause> Clauses = new List<Clause>();

        public static Literal<object> LiteralOf(object obj)
        {
            if (obj == null)
            {
                return NullLiteral.INSTANCE;
            }
            throw new ArgumentException("Unsupported literal type.");
        }

        public static Literal<int> LiteralOf(int obj) => new IntegerLiteral(obj);
        public static Literal<bool> LiteralTrue() => BooleanLiteral.TRUE;
        public static Literal<bool> LiteralFalse() => BooleanLiteral.FALSE;

        public string Print() =>
            throw new NotImplementedException();

        public static CypherQuery ToCypherQuery() =>
            throw new NotImplementedException();
    }
}