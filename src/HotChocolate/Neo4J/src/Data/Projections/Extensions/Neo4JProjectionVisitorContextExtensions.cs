using System.Diagnostics.CodeAnalysis;
using HotChocolate.Data.Neo4J.Language;

#nullable enable

namespace HotChocolate.Data.Neo4J.Projections
{
    internal static class Neo4JProjectionVisitorContextExtensions
    {
        public static bool TryCreateQuery(
            this Neo4JProjectionVisitorContext context,
            [NotNullWhen(true)] out object[]? query)
        {
            query = null;

            if (context.Projections.Count == 0)
            {
                return false;
            }

            query = context.Projections.ToArray();
            return true;
        }

        public static bool TryCreateRelationshipProjection(this Neo4JProjectionVisitorContext context,
            out PatternComprehension patternComprehension)
        {

            patternComprehension = new PatternComprehension(
                 context.Relationships.Peek(),
                 context.EndNodes.Peek().Project(context.RelationshipProjections[context.CurrentLevel].ToArray()));

            return true;
        }
    }
}
