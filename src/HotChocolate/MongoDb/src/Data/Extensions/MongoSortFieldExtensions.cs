using HotChocolate.Data.Sorting;

namespace HotChocolate.Data.MongoDb
{
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
}
