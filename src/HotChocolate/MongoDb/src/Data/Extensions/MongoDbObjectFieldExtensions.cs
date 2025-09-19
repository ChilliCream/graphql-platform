using HotChocolate.Types;

namespace HotChocolate.Data.MongoDb;

internal static class MongoDbObjectFieldExtensions
{
    public static string GetName(this ObjectField field)
        => field.Member?.Name ?? field.Name;
}
