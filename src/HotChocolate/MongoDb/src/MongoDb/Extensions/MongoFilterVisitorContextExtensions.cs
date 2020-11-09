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
            [NotNullWhen(true)] out MongoDbFilterDefinition? query)
        {
            return context.GetMongoFilterScope().TryCreateQuery(out query);
        }
    }
}
