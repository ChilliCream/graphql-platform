using System;
using System.Collections.Generic;
using ServiceStack;

namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// Cypher = [SP], Statement, [[SP], ';'], [SP], EOI ;
    /// Statement = Query ;
    /// Query = RegularQuery | StandaloneCall ;
    /// The main entry point into the Cypher DSL.
    /// The Cypher Builder API is intended for framework usage to produce Cypher statements required for database operations.
    /// </summary>
    public static class Cypher
    {
        /// <summary>
        /// Create a new Node representation with at least one label, the "primary" label. This is required. All other labels
        /// are optional.
        /// </summary>
        /// <param name="primaryLabel">The primary label this node is identified by.</param>
        /// <param name="additionalLabels">Additional labels</param>
        /// <returns>A new node representation</returns>
        public static Node Node(string primaryLabel, params string[] additionalLabels)
        {
            return Language.Node.Create(primaryLabel, additionalLabels);
        }

        public static Node NamedNode(string node) => Node(node).Named(node.ToCamelCase());

        public static Expression LiteralOf<T>(T literal)
        {
            return literal switch
            {
                null => NullLiteral.Instance,
                string s => new StringLiteral(s),
                bool b => b ? BooleanLiteral.True : BooleanLiteral.False,
                short num => new IntegerLiteral(num),
                int num => new IntegerLiteral(num),
                long num => new DoubleLiteral(num),
                float num => new DoubleLiteral(num),
                double num => new DoubleLiteral(num),
                //IList list => new ListLiteral(list),
                _ => throw new ArgumentException("Unsupported literal type")
            };
        }

        public static MapExpression MapOf(params object[] keysAndValues) =>
            MapExpression.Create(keysAndValues);

        public static StatementBuilder Match(params IPatternElement[] pattern) {
            return Statement.Builder().Match(pattern);
        }

        public static StatementBuilder Match(Where optionalWhere, params IPatternElement[] pattern) {
            return Statement.Builder().Match(optionalWhere, pattern);
        }

        public static SymbolicName Name(string value) => SymbolicName.Of(value);
        public static Property Property(Expression expression, string name) => Language.Property.Create(expression, name);
        public static Property Property(string containerName, string name) => Property(Name(containerName), name);
        public static Literal<string> Null() => NullLiteral.Instance;
        public static Literal<bool> LiteralTrue() => BooleanLiteral.True;
        public static Literal<bool> LiteralFalse() => BooleanLiteral.False;
        public static Asterisk Asterisk => Asterisk.Instance;

        public static SortItem Sort(Expression expression) =>
            SortItem.Create(expression, null);
    }
}
