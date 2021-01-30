using HotChocolate.Data.Sorting;

namespace HotChocolate.Data.Neo4J.Sorting
{
    internal static class Neo4JSortFieldExtensions
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
