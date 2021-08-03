using System.Diagnostics.CodeAnalysis;
using System.Linq;
using SqlKata;

namespace HotChocolate.Data.SqlKata.Sorting
{
    /*
    internal static class MongoSortVisitorContextExtensions
    {
        public static string GetPath(this SqlKataSortVisitorContext ctx) =>
            string.Join(".", ctx.Path.Reverse());

        public static bool TryCreateQuery(
            this SqlKataSortVisitorContext context,
            [NotNullWhen(true)] out Query? query)
        {
            query = null;

            if (context.Operations.Count == 0)
            {
                return false;
            }

            //query = new SqlKataCombinedSortDefinition(context.Operations.ToArray());
            return true;
        }
    }
*/
}
