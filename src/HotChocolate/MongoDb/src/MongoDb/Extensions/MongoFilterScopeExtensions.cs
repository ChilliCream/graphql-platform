using System.Diagnostics.CodeAnalysis;
using System.Linq;
using HotChocolate.Data.Filters;
using HotChocolate.MongoDb.Filters.Expressions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HotChocolate.MongoDb
{
    public static class MongoFilterScopeExtensions
    {
        public static string GetPath(this MongoFilterScope scope) =>
            string.Join(".", scope.Path.Reverse());

        public static string GetPath(
            this MongoFilterScope scope,
            IFilterField field)
        {
            scope.Path.Push(field.Name);
            var result = scope.GetPath();
            scope.Path.Pop();
            return result;
        }

        public static bool TryCreateQuery(
            this MongoFilterScope scope,
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
