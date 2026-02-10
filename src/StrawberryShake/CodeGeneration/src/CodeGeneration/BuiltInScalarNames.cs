using HotChocolate.Types;

namespace StrawberryShake.CodeGeneration;

public static class BuiltInScalarNames
{
    private static readonly HashSet<string> s_typeNames =
    [
        "Guid",
        "Uri",
        "URI",
        "Url",
        "Uuid",
        ScalarNames.Any,
        ScalarNames.Boolean,
        ScalarNames.Byte,
        ScalarNames.ByteArray,
        ScalarNames.Date,
        ScalarNames.DateTime,
        ScalarNames.Decimal,
        ScalarNames.Float,
        ScalarNames.ID,
        ScalarNames.Int,
        ScalarNames.LocalDate,
        ScalarNames.LocalDateTime,
        ScalarNames.LocalTime,
        ScalarNames.Long,
        ScalarNames.Short,
        ScalarNames.String,
        ScalarNames.TimeSpan,
        ScalarNames.UnsignedByte,
        ScalarNames.UnsignedInt,
        ScalarNames.UnsignedLong,
        ScalarNames.UnsignedShort,
        ScalarNames.URL,
        ScalarNames.UUID
    ];

    public static bool IsBuiltInScalar(string typeName) => s_typeNames.Contains(typeName);
}
