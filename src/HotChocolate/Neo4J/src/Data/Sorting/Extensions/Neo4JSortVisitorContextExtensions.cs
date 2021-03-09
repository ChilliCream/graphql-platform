using System.Diagnostics.CodeAnalysis;
using System.Linq;
using HotChocolate.Data.Neo4J.Language;

namespace HotChocolate.Data.Neo4J.Sorting
{
    internal static class Neo4JSortVisitorContextExtensions
    {
        public static string GetPath(this Neo4JSortVisitorContext ctx) =>
            string.Join(".", ctx.Path.Reverse());

        public static bool TryCreateQuery(
            this Neo4JSortVisitorContext context,
            [NotNullWhen(true)] out OrderBy? query)
        {
            query = null;

            if (context.Operations.Count == 0)
            {
                return false;
            }

            query = new OrderBy(context.Operations.ToList());
            return true;
        }
    }
}
