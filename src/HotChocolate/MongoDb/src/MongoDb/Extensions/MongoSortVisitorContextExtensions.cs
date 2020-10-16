using System.Diagnostics.CodeAnalysis;
using System.Linq;
using HotChocolate.MongoDb.Sorting.Convention.Extensions.Handlers;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HotChocolate.MongoDb.Data.Sorting
{
    internal static class MongoSortVisitorContextExtensions
    {
        public static string GetPath(this MongoDbSortVisitorContext ctx) =>
            string.Join(".", ctx.Path.Reverse());

        public static bool TryCreateQuery(
            this MongoDbSortVisitorContext context,
            [NotNullWhen(true)] out BsonDocument? query)
        {
            query = null;

            if (context.Operations.Count == 0)
            {
                return false;
            }

            query = Builders<BsonDocument>.Sort.Combine(context.Operations).DefaultRender();
            return true;
        }
    }
}
