using System.Collections.Generic;
using HotChocolate.Types;

namespace StrawberryShake.CodeGeneration;

public static class BuiltInScalarNames
{
    private static readonly HashSet<string> _typeNames =
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
        ScalarNames.Name,
        ScalarNames.ByteArray,
        ScalarNames.Any,
        ScalarNames.JSON,
        ScalarNames.TimeSpan,
    ];

    public static bool IsBuiltInScalar(string typeName) => _typeNames.Contains(typeName);
}
