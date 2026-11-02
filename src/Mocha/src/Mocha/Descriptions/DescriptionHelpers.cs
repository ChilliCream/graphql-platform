namespace Mocha;

internal static class DescriptionHelpers
{
    public static string GetTypeName(Type type)
    {
        if (!type.IsGenericType)
        {
            return type.Name;
        }

        var genericTypeName = type.Name.Split('`')[0];
        var genericArgs = string.Join(", ", type.GetGenericArguments().Select(GetTypeName));
        return $"{genericTypeName}<{genericArgs}>";
    }
}
