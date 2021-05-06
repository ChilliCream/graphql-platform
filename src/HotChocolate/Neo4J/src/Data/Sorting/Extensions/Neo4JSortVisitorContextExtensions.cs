using System.Diagnostics.CodeAnalysis;
using HotChocolate.Data.Neo4J.Language;

namespace HotChocolate.Data.Neo4J.Sorting
{
    internal static class Neo4JSortVisitorContextExtensions
    {
        public static bool TryCreateQuery(
            this Neo4JSortVisitorContext context,
            [NotNullWhen(true)] out Neo4JSortDefinition[] query)
        {
            query = null;

            if (context.Operations.Count == 0)
            {
                return false;
            }

            query = context.Operations.ToArray();
            return true;
        }
    }
}
