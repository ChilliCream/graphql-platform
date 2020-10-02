using HotChocolate.Data.Filters;

namespace HotChocolate.MongoDb
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
}
