using System.Globalization;

namespace HotChocolate.Properties;

internal static class TypeResourceHelper
{
    public static string Scalar_Cannot_Serialize(string typeName)
    {
        ArgumentException.ThrowIfNullOrEmpty(typeName);

        return string.Format(
            CultureInfo.InvariantCulture,
            TypeResources.Scalar_Cannot_Serialize,
            typeName);
    }

    public static string Scalar_Cannot_Deserialize(string typeName)
    {
        ArgumentException.ThrowIfNullOrEmpty(typeName);

        return string.Format(
            CultureInfo.InvariantCulture,
            TypeResources.Scalar_Cannot_Deserialize,
            typeName);
    }

    public static string Scalar_Cannot_ParseLiteral(
        string typeName, Type literalType)
    {
        ArgumentException.ThrowIfNullOrEmpty(typeName);
        ArgumentNullException.ThrowIfNull(literalType);

        return string.Format(
            CultureInfo.InvariantCulture,
            TypeResources.Scalar_Cannot_ParseLiteral,
            typeName,
            literalType.Name);
    }

    public static string Scalar_Cannot_ParseValue(
        string typeName, Type valueType)
    {
        ArgumentException.ThrowIfNullOrEmpty(typeName);
        ArgumentNullException.ThrowIfNull(valueType);

        return string.Format(
            CultureInfo.InvariantCulture,
            TypeResources.Scalar_Cannot_ParseValue,
            typeName,
            valueType.FullName);
    }

    public static string Scalar_Cannot_ParseResult(
        string typeName, Type valueType)
    {
        ArgumentException.ThrowIfNullOrEmpty(typeName);
        ArgumentNullException.ThrowIfNull(valueType);

        return string.Format(
            CultureInfo.InvariantCulture,
            TypeResources.Scalar_Cannot_ParseValue,
            typeName,
            valueType.FullName);
    }

    public static string Type_Name_IsNotValid(string typeName)
    {
        var name = typeName ?? "null";
        return $"`{name}` is not a valid "
            + "GraphQL type name.";
    }
}
