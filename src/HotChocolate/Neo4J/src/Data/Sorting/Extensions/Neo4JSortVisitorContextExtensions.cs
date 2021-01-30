using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace HotChocolate.Data.Neo4J.Sorting
{
    internal static class Neo4JSortVisitorContextExtensions
    {
        public static string GetPath(this Neo4JSortVisitorContext ctx) =>
            string.Join(".", ctx.Path.Reverse());

        public static bool TryCreateQuery(
            this Neo4JSortVisitorContext context,
            [NotNullWhen(true)] out Neo4JSortDefinition? query)
        {
            query = null;

            if (context.Operations.Count == 0)
            {
                return false;
            }

            query = new Neo4JCombinedSortDefinition(context.Operations.ToArray());
            return true;
        }
    }
}
