using HotChocolate.Data.Filters;

namespace HotChocolate.Data.ElasticSearch;

public static class ElasticSearchFilterOperationFieldExtensions
{
    public static string GetName(this IFilterField field)
    {
        string fieldName = field.Name;
        if (field.Member is { } p)
        {
            fieldName = p.Name;
        }

        return fieldName;
    }
}
