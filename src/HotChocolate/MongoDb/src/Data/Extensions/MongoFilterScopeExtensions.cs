using System.Diagnostics.CodeAnalysis;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HotChocolate.Data.MongoDb.Filters
{
    internal static class MongoFilterScopeExtensions
    {
        public static string GetPath(this MongoDbFilterScope scope) =>
            string.Join(".", scope.Path.Reverse());

        public static bool TryCreateQuery(
            this MongoDbFilterScope scope,
            [NotNullWhen(true)] out MongoDbFilterDefinition? query)
        {
            query = null;

            if (scope.Level.Peek().Count == 0)
            {
                return false;
            }

            query = new AndFilterDefinition(scope.Level.Peek().ToArray());

            return true;
        }
    }
}
