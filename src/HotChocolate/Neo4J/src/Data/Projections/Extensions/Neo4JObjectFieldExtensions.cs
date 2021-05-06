using HotChocolate.Types;

namespace HotChocolate.Data.Neo4J.Projections
{
    public static class Neo4JObjectFieldExtensions
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
