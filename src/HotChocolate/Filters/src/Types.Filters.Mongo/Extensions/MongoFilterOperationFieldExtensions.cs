namespace HotChocolate.Types.Filters.Mongo.Extensions
{
    public static class MongoFilterOperationFieldExtensions
    {
        public static string GetName(
            this FilterOperationField field)
        {
            string fieldName = field.Name;
            if (field.Operation?.Property is { } p)
            {
                fieldName = p.Name;
            }
            return fieldName;
        }
    }
}
