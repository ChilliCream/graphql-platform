using HotChocolate.Types;

namespace StrawberryShake.CodeGeneration;

public static class BuiltInScalarNames
{
    private static readonly HashSet<string> s_typeNames =
    [
        ScalarNames.String,
        ScalarNames.ID,
        ScalarNames.Boolean,
        ScalarNames.Byte,
        ScalarNames.Short,
        ScalarNames.Int,
        ScalarNames.Long,
        ScalarNames.Float,
        ScalarNames.Decimal,
        ScalarNames.URL,
        "Url",
        "URI",
        "Uri",
        ScalarNames.UUID,
        "Uuid",
        "Guid",
        ScalarNames.DateTime,
        ScalarNames.Date,
        ScalarNames.LocalDate,
        ScalarNames.LocalDateTime,
        ScalarNames.LocalTime,
        ScalarNames.ByteArray,
        ScalarNames.Any,
        ScalarNames.TimeSpan
    ];

    public static bool IsBuiltInScalar(string typeName) => s_typeNames.Contains(typeName);
}
