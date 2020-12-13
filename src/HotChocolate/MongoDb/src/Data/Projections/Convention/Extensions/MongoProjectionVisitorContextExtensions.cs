using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace HotChocolate.Data.MongoDb
{
    internal static class MongoProjectionVisitorContextExtensions
    {
        public static string GetPath(this MongoDbProjectionVisitorContext ctx) =>
            string.Join(".", ctx.Path.Reverse());

        public static bool TryCreateQuery(
            this MongoDbProjectionVisitorContext context,
            [NotNullWhen(true)] out MongoDbProjectionDefinition? query)
        {
            query = null;

            if (context.Projections.Count == 0)
            {
                return false;
            }

            query = new MongoDbCombinedProjectionDefinition(context.Projections.ToArray());
            return true;
        }
    }
}
