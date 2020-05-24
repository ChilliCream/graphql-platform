using System.Diagnostics.CodeAnalysis;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HotChocolate.Types.Filters.Mongo
{
    public static class MongoFilterScopeExtensions
    {
        public static string GetPath(this MongoFilterScope scope) =>
            string.Join(".", scope.Path);

        public static bool TryCreateQuery(
            this MongoFilterScope scope,
            [NotNullWhen(true)]out FilterDefinition<BsonDocument>? query)
        {
            query = null;

            if (scope.Level.Peek().Count == 0)
            {
                return false;
            }

            query = scope.Context.Builder.And(scope.Level.Peek().ToArray());

            return true;
        }
    }
}
