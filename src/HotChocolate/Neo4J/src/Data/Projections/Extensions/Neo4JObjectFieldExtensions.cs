using HotChocolate.Types;

namespace HotChocolate.Data.Neo4J.Projections;

internal static class Neo4JObjectFieldExtensions
{
    public static string GetName(
        this IObjectField field)
    {
        var fieldName = field.Name;
        if (field.Member is { } p)
        {
            fieldName = p.Name;
        }

        return fieldName;
    }
}