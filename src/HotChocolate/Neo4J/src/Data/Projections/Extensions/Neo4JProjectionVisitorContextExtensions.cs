using System.Diagnostics.CodeAnalysis;
using System.Linq;

#nullable enable

namespace HotChocolate.Data.Neo4J.Projections
{
    internal static class Neo4JProjectionVisitorContextExtensions
    {
        public static string GetPath(this Neo4JProjectionVisitorContext ctx) =>
            string.Join(".", ctx.Path.Reverse());

        public static bool TryCreateQuery(
            this Neo4JProjectionVisitorContext context,
            [NotNullWhen(true)] out Neo4JProjectionDefinition? query)
        {
            query = null;

            if (context.Projections.Count == 0)
            {
                return false;
            }

            query = new Neo4JCombinedProjectionDefinition(context.Projections.ToArray());
            return true;
        }
    }
}
