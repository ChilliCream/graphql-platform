using System;
using System.Collections.Generic;
using HotChocolate.Data.Neo4J.Language;

namespace HotChocolate.Data.Neo4J
{
    /// <summary>
    /// The main entry point into the Cypher DSL.
    /// The Cypher Builder API is intended for framework usage to produce Cypher statements required for database operations.
    /// </summary>
    public static class Cypher
    {
        public static Node Node(string primaryLabel)
        {
            return Language.Node.Create(primaryLabel);
        }

        public static Node Node(string primaryLabel, string[] additionalLabels)
        {
            return Language.Node.Create(primaryLabel, additionalLabels);
        }

        //public static CypherBuilder Create(IPatternElement[] pattern) => CypherBuilder.Builder().Create(pattern);

        public static Literal<string> Null() => NullLiteral.Instance;
        public static Literal<string> StringLiteral(string str) => new StringLiteral(str);
        public static Literal<bool> LiteralTrue() => BooleanLiteral.True;
        public static Literal<bool> LiteralFalse() => BooleanLiteral.False;
        public static Literal<string> Asterik() => Asterisk.Instance;
    }
}
