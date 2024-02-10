using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Data.MongoDb.Sorting;

public static class MongoSortVisitorContextExtensions
{
    public static string GetPath(this MongoDbSortVisitorContext ctx) =>
        string.Join(".", ctx.Path.Reverse());

    public static bool TryCreateQuery(
        this MongoDbSortVisitorContext context,
        [NotNullWhen(true)] out MongoDbSortDefinition? query)
    {
        query = null;

        if (context.Operations.Count == 0)
        {
            return false;
        }

        query = new MongoDbCombinedSortDefinition(context.Operations.ToArray());
        return true;
    }
}
