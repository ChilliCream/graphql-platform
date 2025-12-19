using System.Globalization;
using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Properties;

internal static class TypeResourceHelper
{
    public static string Scalar_Cannot_CoerceInputLiteral(ScalarType scalarType, IValueNode valueLiteral)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            TypeResources.Scalar_Cannot_CoerceInputLiteral,
            typeName,
            literalType.Name);
    }

    public static string Scalar_Cannot_CoerceInputValue(ScalarType scalarType, JsonElement inputValue)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            TypeResources.Scalar_Cannot_CoerceInputValue,
            typeName,
            valueKind);
    }

    public static string Scalar_Cannot_ConvertValueToLiteral(ScalarType scalarType, object runtimeValue)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            TypeResources.Scalar_Cannot_Deserialize,
            typeName);
    }

    public static string Scalar_Cannot_CoerceOutputValue(ScalarType scalarType, object runtimeValue)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            TypeResources.Scalar_Cannot_Serialize,
            typeName);
    }

    public static string Type_Name_IsNotValid(string typeName)
    {
        var name = typeName ?? "null";
        return $"`{name}` is not a valid GraphQL type name.";
    }
}
