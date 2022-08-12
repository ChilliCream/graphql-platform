using HotChocolate.Data.Filters;

namespace HotChocolate.Data.MongoDb;

public static class MongoFilterOperationFieldExtensions
{
    public static string GetName(
        this IFilterField field)
    {
        var fieldName = field.Name;
        if (field.Member is { } p)
        {
            fieldName = p.Name;
        }

        return fieldName;
    }
}
