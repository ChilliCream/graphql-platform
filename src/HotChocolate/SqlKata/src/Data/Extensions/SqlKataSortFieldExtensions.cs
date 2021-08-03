using HotChocolate.Data.Sorting;

namespace HotChocolate.Data.SqlKata
{
    internal static class SqlKataSortFieldExtensions
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
