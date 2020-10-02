using System.Diagnostics.CodeAnalysis;
using HotChocolate.Data.Filters;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HotChocolate.MongoDb.Data.Filters
{
    public static class MongoFilterVisitorContextExtensions
    {
        public static MongoDbFilterScope GetMongoFilterScope(
            this MongoDbFilterVisitorContext context) =>
            (MongoDbFilterScope)context.GetScope();

        public static bool TryCreateQuery(
            this MongoDbFilterVisitorContext context,
            [NotNullWhen(true)] out BsonDocument? query)
        {
            if (context.GetMongoFilterScope()
                .TryCreateQuery(
                    out FilterDefinition<BsonDocument>? bsonQuery))
            {
                query = bsonQuery.DefaultRender();
                return true;
            }

            query = null;
            return false;
        }
    }
}
