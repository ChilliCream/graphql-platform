using System.Diagnostics.CodeAnalysis;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HotChocolate.MongoDb.Data.Filters
{
    internal static class MongoFilterScopeExtensions
    {
        public static string GetPath(this MongoDbFilterScope scope) =>
            string.Join(".", scope.Path.Reverse());

        public static bool TryCreateQuery(
            this MongoDbFilterScope scope,
            [NotNullWhen(true)] out FilterDefinition<BsonDocument>? query)
        {
            query = null;

            if (scope.Level.Peek().Count == 0)
            {
                return false;
            }

            query = scope.Context.Builder.And(
                scope.Level.Peek().ToArray());

            return true;
        }
    }
}
