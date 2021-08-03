using System.Diagnostics.CodeAnalysis;
using System.Linq;
using SqlKata;

namespace HotChocolate.Data.SqlKata.Filters
{
    internal static class SqlKataFilterScopeExtensions
    {
        public static string GetPath(this SqlKataFilterScope scope) =>
            string.Join(".", scope.Path.Reverse());

        public static bool TryCreateQuery(
            this SqlKataFilterScope scope,
            [NotNullWhen(true)] out Query? query)
        {
            query = null;

            if (scope.Level.Peek().Count == 0)
            {
                return false;
            }

            query = new Query();
            query.Clauses.AddRange(scope.Level.Peek().SelectMany(x => x.Clauses));

            return true;
        }
    }
}
