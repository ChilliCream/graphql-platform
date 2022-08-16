using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace HotChocolate.Data.MongoDb.Filters
{
    public static class MongoDbFilterScopeExtensions
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
                query = MongoDbFilterDefinition.Empty;
                return true;
            }

            query = new AndFilterDefinition(scope.Level.Peek().ToArray());

            return true;
        }
    }
}
