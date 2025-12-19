using System.Globalization;

namespace HotChocolate.Properties;

internal static class TypeResourceHelper
{
    public static string Scalar_Cannot_ConvertValueToLiteral(Type actualType, string actualValue)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            TypeResources.Scalar_Cannot_Deserialize,
            typeName);
    }

    public static string Scalar_Cannot_Serialize(string typeName)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            TypeResources.Scalar_Cannot_Serialize,
            typeName);
    }

    public static string Scalar_Cannot_Deserialize(string typeName)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            TypeResources.Scalar_Cannot_Deserialize,
            typeName);
    }

    public static string Scalar_Cannot_CoerceInputLiteral(string typeName, Type literalType)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            TypeResources.Scalar_Cannot_CoerceInputLiteral,
            typeName,
            literalType.Name);
    }

    public static string Scalar_Cannot_CoerceInputValue(string typeName, Type valueType)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            TypeResources.Scalar_Cannot_CoerceInputValue,
            typeName,
            valueType.FullName);
    }

    public static string Scalar_Cannot_ParseResult(string typeName, Type valueType)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            TypeResources.Scalar_Cannot_CoerceInputValue,
            typeName,
            valueType.FullName);
    }

    public static string Type_Name_IsNotValid(string typeName)
    {
        var name = typeName ?? "null";
        return $"`{name}` is not a valid GraphQL type name.";
    }
}
