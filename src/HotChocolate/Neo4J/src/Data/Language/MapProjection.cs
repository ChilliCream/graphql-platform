using System;
using System.Collections.Generic;

#nullable enable

namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// https://medium.com/neo4j/loading-graph-data-for-an-object-graph-mapper-or-graphql-5103b1a8b66e
    /// </summary>
    public class MapProjection : Expression
    {
        public override ClauseKind Kind => ClauseKind.MapProjection;
        private readonly SymbolicName _name;
        private readonly MapExpression _expression;

        public MapProjection(SymbolicName name, MapExpression expression)
        {
            _name = name;
            _expression = expression;
        }

        public static MapProjection Create(SymbolicName name, params object[] content) =>
            new(name, MapExpression.WithEntries(CreateNewContent(content)));

        private static object? ContentAt(object[] content, int i)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));

            object currentObject = content[i];
            return currentObject switch
            {
                Expression expression => Expressions.NameOrExpression(expression),
                INamed named => named.GetSymbolicName(),
                _ => currentObject
            };
        }

        private static List<Expression> CreateNewContent(params object[] content)
        {
            List<Expression> newContent = new();
            HashSet<string> knownKeys = new();

            string? lastKey = null;
            Expression? lastExpression = null;
            int i = 0;
            while (i < content.Length)
            {
                object? next;
                if (i + 1 >= content.Length)
                    next = null;
                else
                    next = ContentAt(content, i + 1);

                object? current = ContentAt(content, i);

                if (current is string)
                {

                }

            }

            return newContent;
        }

        public override void Visit(CypherVisitor cypherVisitor)
        {
            cypherVisitor.Enter(this);
            _name.Visit(cypherVisitor);
            _expression.Visit(cypherVisitor);
            cypherVisitor.Leave(this);
        }
    }
}
