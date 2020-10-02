using HotChocolate.Data.Filters;

namespace HotChocolate.MongoDb
{
    public static class MongoFilterOperationFieldExtensions
    {
        public static string GetName(
            this FilterOperationField field)
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
