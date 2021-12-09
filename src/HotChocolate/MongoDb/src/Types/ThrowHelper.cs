using HotChocolate.Language;
using HotChocolate.Language.Utilities;
using HotChocolate.Types.MongoDb.Resources;

namespace HotChocolate.Types.MongoDb;

internal static class ThrowHelper
{
    public static SerializationException Bson_CouldNotParseValue(
        ScalarType type,
        object? value) =>
        new(
            string.Format(
                MongoDbTypesResources.Bson_Type_CouldNotParseValue,
                value?.ToString() ?? "null"),
            type);

    public static SerializationException Bson_CouldNotParseLiteral(
        ScalarType type,
        IValueNode literal) =>
        new(
            string.Format(MongoDbTypesResources.Bson_Type_CouldNotParseLiteral, literal.Print()),
            type);
}
