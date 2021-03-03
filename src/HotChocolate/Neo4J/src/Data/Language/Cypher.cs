using System;
using System.Collections;

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

        public static Node Node(string primaryLabel, MapExpression properties, params string[] additionalLabels)
        {
            return Language.Node.Create(primaryLabel, properties, additionalLabels);
        }

        public static Node AnyNode() => Language.Node.Create();

        public static Expression LiteralOf<T>(T literal)
        {
            return literal switch
            {
                null => NullLiteral.Instance,
                string s => new StringLiteral(s),
                bool b => b ? BooleanLiteral.True : BooleanLiteral.False,
                int num => new IntegerLiteral(num),
                double num => new DoubleLiteral(num),
                //IEnumerable list => new ListLiteral(num),
                _ => throw new ArgumentException("Unsupported literal type")
            };
        }

        public static MapExpression MapOf(params object[] keysAndValues) =>
            MapExpression.Create(keysAndValues);

        public static StatementBuilder Match(params IPatternElement[] pattern) {
            return Statement.Builder().Match(pattern);
        }

        public static StatementBuilder Create(params IPatternElement[] patternElements)
        {
            return Statement.Builder().Create(patternElements);
        }

        public static SymbolicName Name(string value) => SymbolicName.Of(value);
        public static Property Property(Expression expression, string name) => Language.Property.Create(expression, name);
        public static Property Property(string containerName, string name) => Property(Name(containerName), name);
        public static Literal<string> Null() => NullLiteral.Instance;
        public static Literal<bool> LiteralTrue() => BooleanLiteral.True;
        public static Literal<bool> LiteralFalse() => BooleanLiteral.False;
        public static Asterisk Asterisk => Asterisk.Instance;
    }
}
