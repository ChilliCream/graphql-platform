using System.Diagnostics.CodeAnalysis;
using HotChocolate.Data.Filters;
using HotChocolate.MongoDb.Filters.Expressions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HotChocolate.MongoDb.Data
{
    public static class MongoFilterVisitorContextExtensions
    {
        public static MongoFilterScope GetMongoFilterScope(
            this MongoFilterVisitorContext context) =>
            (MongoFilterScope)context.GetScope();

        public static bool TryCreateQuery(
            this MongoFilterVisitorContext context,
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
