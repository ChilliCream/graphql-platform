using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace HotChocolate.Types;

/// <summary>
/// This class provides helper methods to deal with scalar types.
/// </summary>
public static class Scalars
{
    private static readonly Dictionary<Type, Type> s_lookup = new()
    {
        { typeof(string), typeof(StringType) },
        { typeof(bool), typeof(BooleanType) },
        { typeof(byte), typeof(UnsignedByteType) },
        { typeof(short), typeof(ShortType) },
        { typeof(int), typeof(IntType) },
        { typeof(long), typeof(LongType) },
        { typeof(sbyte), typeof(ByteType) },

        { typeof(float), typeof(FloatType) },
        { typeof(double), typeof(FloatType) },
        { typeof(decimal), typeof(DecimalType) },

        { typeof(Uri), typeof(UriType) },
        { typeof(Guid), typeof(UuidType) },
        { typeof(DateTime), typeof(DateTimeType) },
        { typeof(DateTimeOffset), typeof(DateTimeType) },
        { typeof(byte[]), typeof(Base64StringType) },
        { typeof(TimeSpan), typeof(TimeSpanType) },

        { typeof(DateOnly), typeof(LocalDateType) },
        { typeof(TimeOnly), typeof(LocalTimeType) },
        { typeof(JsonElement), typeof(AnyType) }
    };

    private static readonly Dictionary<string, Type> s_nameLookup = new()
    {
        { ScalarNames.String, typeof(StringType) },
        { ScalarNames.ID, typeof(IdType) },
        { ScalarNames.Boolean, typeof(BooleanType) },
        { ScalarNames.Byte, typeof(ByteType) },
        { ScalarNames.UnsignedByte, typeof(UnsignedByteType) },
        { ScalarNames.Short, typeof(ShortType) },
        { ScalarNames.Int, typeof(IntType) },
        { ScalarNames.Long, typeof(LongType) },

        { ScalarNames.Float, typeof(FloatType) },
        { ScalarNames.Decimal, typeof(DecimalType) },

        { ScalarNames.URI, typeof(UriType) },
        { ScalarNames.URL, typeof(UrlType) },
        { ScalarNames.UUID, typeof(UuidType) },
        { ScalarNames.DateTime, typeof(DateTimeType) },
        { ScalarNames.Date, typeof(DateType) },
        { ScalarNames.TimeSpan, typeof(TimeSpanType) },
        { ScalarNames.Any, typeof(AnyType) },
        { ScalarNames.LocalDate, typeof(LocalDateType) },
        { ScalarNames.LocalDateTime, typeof(LocalDateTimeType) },
        { ScalarNames.LocalTime, typeof(LocalTimeType) },

        { ScalarNames.Base64String, typeof(Base64StringType) },
#pragma warning disable CS0618 // Type or member is obsolete
        { ScalarNames.ByteArray, typeof(ByteArrayType) }
#pragma warning restore CS0618 // Type or member is obsolete
    };

    private static readonly Dictionary<Type, ValueKind> s_scalarKinds = new()
    {
        { typeof(string), ValueKind.String },
        { typeof(long), ValueKind.Integer },
        { typeof(int), ValueKind.Integer },
        { typeof(short), ValueKind.Integer },
        { typeof(long?), ValueKind.Integer },
        { typeof(int?), ValueKind.Integer },
        { typeof(short?), ValueKind.Integer },
        { typeof(ulong), ValueKind.Integer },
        { typeof(uint), ValueKind.Integer },
        { typeof(ushort), ValueKind.Integer },
        { typeof(ulong?), ValueKind.Integer },
        { typeof(uint?), ValueKind.Integer },
        { typeof(ushort?), ValueKind.Integer },
        { typeof(byte), ValueKind.Integer },
        { typeof(byte?), ValueKind.Integer },
        { typeof(sbyte), ValueKind.Integer },
        { typeof(sbyte?), ValueKind.Integer },
        { typeof(float), ValueKind.Float },
        { typeof(double), ValueKind.Float },
        { typeof(decimal), ValueKind.Float },
        { typeof(float?), ValueKind.Float },
        { typeof(double?), ValueKind.Float },
        { typeof(decimal?), ValueKind.Float },
        { typeof(bool), ValueKind.Float },
        { typeof(bool?), ValueKind.Float }
    };

    private static readonly HashSet<string> s_specScalars =
    [
        ScalarNames.ID,
        ScalarNames.String,
        ScalarNames.Int,
        ScalarNames.Float,
        ScalarNames.Boolean
    ];

    internal static bool TryGetScalar(
        Type runtimeType,
        [NotNullWhen(true)] out Type? schemaType) =>
        s_lookup.TryGetValue(
            runtimeType ?? throw new ArgumentNullException(nameof(runtimeType)),
            out schemaType);

    internal static bool TryGetScalar(
        string typeName,
        [NotNullWhen(true)] out Type? schemaType)
    {
        ArgumentException.ThrowIfNullOrEmpty(typeName);

        return s_nameLookup.TryGetValue(typeName, out schemaType);
    }

    /// <summary>
    /// Defines if the specified name represents a built-in scalar type.
    /// </summary>
    /// <param name="typeName">
    /// A GraphQL type name.
    /// </param>
    /// <returns>
    /// Returns <c>true</c> if the specified name represents a built-in scalar type;
    /// otherwise, <c>false</c>.
    /// </returns>
    public static bool IsBuiltIn(string typeName)
        => !string.IsNullOrEmpty(typeName)
            && s_nameLookup.ContainsKey(typeName);

    /// <summary>
    /// Tries to infer the GraphQL literal kind from a runtime value.
    /// </summary>
    /// <param name="value">
    /// The runtime value.
    /// </param>
    /// <param name="kind">
    /// The expected GraphQL literal kind.
    /// </param>
    /// <returns>
    /// <c>true</c> if the literal kind can be inferred.
    /// </returns>
    public static bool TryGetKind(object? value, out ValueKind kind)
    {
        if (value is null)
        {
            kind = ValueKind.Null;
            return true;
        }

        var valueType = value.GetType();

        if (valueType.IsEnum)
        {
            kind = ValueKind.Enum;
            return true;
        }

        if (s_scalarKinds.TryGetValue(valueType, out kind))
        {
            return true;
        }

        if (value is IDictionary)
        {
            kind = ValueKind.Object;
            return true;
        }

        if (value is ICollection)
        {
            kind = ValueKind.List;
            return true;
        }

        kind = ValueKind.Unknown;
        return false;
    }

    internal static bool IsSpec(string typeName)
        => s_specScalars.Contains(typeName);
}
