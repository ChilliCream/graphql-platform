using HotChocolate.Data.Filters;

namespace HotChocolate.Data.Neo4J.Filtering
{
    internal static class Neo4JFilterFieldExtensions
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
