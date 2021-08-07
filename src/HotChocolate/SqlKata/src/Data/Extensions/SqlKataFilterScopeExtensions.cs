using System.Diagnostics.CodeAnalysis;
using System.Linq;
using SqlKata;

namespace HotChocolate.Data.SqlKata.Filters
{
    internal static class SqlKataFilterScopeExtensions
    {
        public static string GetColumnName(this SqlKataFilterScope scope) =>
            $"{scope.TableInfo.Peek().Alias}.{scope.Fields.Peek().GetColumnName()}";

        public static string GetForeignKey(this SqlKataFilterScope scope) =>
            $"{scope.TableInfo.Peek().Alias}.{scope.Fields.Peek().GetForeignKey()}";

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
