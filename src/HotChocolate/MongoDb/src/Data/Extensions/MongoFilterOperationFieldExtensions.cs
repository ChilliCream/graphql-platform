using HotChocolate.Data.Filters;
using HotChocolate.Data.Sorting;
using HotChocolate.Types;

namespace HotChocolate.Data.MongoDb
{
    internal static class MongoFilterOperationFieldExtensions
    {
        public static string GetName(
            this IFilterField field)
        {
            string fieldName = field.Name;
            if (field.Member is { } p)
            {
                fieldName = p.Name;
            }

            return fieldName;
        }
    }

    internal static class MongoSortFieldExtensions
    {
        public static string GetName(
            this ISortField field)
        {
            string fieldName = field.Name;
            if (field.Member is { } p)
            {
                fieldName = p.Name;
            }

            return fieldName;
        }
    }

    internal static class MongoObjectFieldExtensions
    {
        public static string GetName(
            this IObjectField field)
        {
            string fieldName = field.Name;
            if (field.Member is { } p)
            {
                fieldName = p.Name;
            }

            return fieldName;
        }
    }
}
