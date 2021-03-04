using System;
using System.Collections.Generic;

#nullable enable

namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// Represents a map projection as described in
    /// https://medium.com/neo4j/loading-graph-data-for-an-object-graph-mapper-or-graphql-5103b1a8b66e
    /// </summary>
    public class MapProjection : Expression
    {
        public override ClauseKind Kind => ClauseKind.MapProjection;
        private readonly SymbolicName _name;
        private readonly MapExpression _expression;

        private MapProjection(SymbolicName name, MapExpression expression)
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
            var i = 0;
            while (i < content.Length)
            {
                object? next = i + 1 >= content.Length ? null : ContentAt(content, i + 1);
                object? current = ContentAt(content, i);

                switch (current)
                {
                    case string s when next is Expression expression:
                        lastKey = s;
                        lastExpression = expression;
                        i += 2;
                        break;
                    case string s:
                        lastKey = null;
                        lastExpression = PropertyLookup.ForName(s);
                        i += 1;
                        break;
                    case Expression expression:
                        lastKey = null;
                        lastExpression = expression;
                        i += 1;
                        break;
                }

                if (lastExpression is Asterisk)
                {
                    lastExpression = PropertyLookup.Wildcard();
                }

                if (lastKey != null)
                {
                    Ensure.IsTrue(!knownKeys.Contains(lastKey), "Duplicate key '" + lastKey + "'");
                    newContent.Add(new KeyValueMapEntry(lastKey, lastExpression));
                    knownKeys.Add(lastKey);
                }
                else switch (lastExpression)
                {
                    case SymbolicName:
                    case PropertyLookup:
                        newContent.Add(lastExpression);
                        break;
                    case Property property:
                    {
                        List<PropertyLookup> names = property.GetNames();
                        if (names.Count > 1)
                        {
                            throw new InvalidOperationException("Cannot project nested properties");
                        }
                        newContent.AddRange(names);
                        break;
                    }
                    case AliasedExpression expression:
                    {
                        newContent.Add(new KeyValueMapEntry(expression.GetAlias(), expression));
                        break;
                    }
                    case null:
                        throw new InvalidOperationException("Could not determine an expression from the given content!");
                    default:
                        throw new InvalidOperationException(lastExpression + " of type " + " cannot be used with an implicit name as map entry.");
                }
                lastKey = null;
                lastExpression = null;
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
