using HotChocolate.Data.Sorting;

namespace HotChocolate.Data.MongoDb;

public static class MongoDbSortFieldExtensions
{
    public static string GetName(
        this ISortField field)
    {
        var fieldName = field.Name;
        if (field.Member is { } p)
        {
            fieldName = p.Name;
        }

        return fieldName;
    }
}
