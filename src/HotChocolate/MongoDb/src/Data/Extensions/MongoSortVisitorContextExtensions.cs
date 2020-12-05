using System.Diagnostics.CodeAnalysis;
using System.Linq;
using HotChocolate.Data.MongoDb.Sorting.Convention.Extensions.Handlers;

namespace HotChocolate.Data.MongoDb.Sorting
{
    internal static class MongoSortVisitorContextExtensions
    {
        public static string GetPath(this MongoDbSortVisitorContext ctx) =>
            string.Join(".", ctx.Path.Reverse());

        public static bool TryCreateQuery(
            this MongoDbSortVisitorContext context,
            [NotNullWhen(true)] out MongoDbSortDefinition? query)
        {
            query = null;

            if (context.Operations.Count == 0)
            {
                return false;
            }

            query = new MongoDbCombinedSortDefinition(context.Operations.ToArray());
            return true;
        }
    }
}
