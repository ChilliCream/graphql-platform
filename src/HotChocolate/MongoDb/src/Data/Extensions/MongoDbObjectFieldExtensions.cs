using HotChocolate.Types;

namespace HotChocolate.Data.MongoDb
{
    internal static class MongoDbObjectFieldExtensions
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
