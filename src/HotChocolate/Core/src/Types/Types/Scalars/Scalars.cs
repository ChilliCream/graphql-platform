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
        { typeof(bool), typeof(BooleanType) },
        { typeof(byte), typeof(UnsignedByteType) },
        { typeof(byte[]), typeof(Base64StringType) },
        { typeof(DateOnly), typeof(LocalDateType) },
        { typeof(DateTime), typeof(DateTimeType) },
        { typeof(DateTimeOffset), typeof(DateTimeType) },
        { typeof(Dictionary<string, object>), typeof(AnyType) },
        { typeof(decimal), typeof(DecimalType) },
        { typeof(double), typeof(FloatType) },
        { typeof(float), typeof(FloatType) },
        { typeof(Guid), typeof(UuidType) },
        { typeof(IDictionary<string, object>), typeof(AnyType) },
        { typeof(int), typeof(IntType) },
        { typeof(IReadOnlyDictionary<string, object>), typeof(AnyType) },
        { typeof(JsonElement), typeof(AnyType) },
        { typeof(long), typeof(LongType) },
        { typeof(object), typeof(AnyType) },
        { typeof(sbyte), typeof(ByteType) },
        { typeof(short), typeof(ShortType) },
        { typeof(string), typeof(StringType) },
        { typeof(TimeOnly), typeof(LocalTimeType) },
        { typeof(TimeSpan), typeof(TimeSpanType) },
        { typeof(uint), typeof(UnsignedIntType) },
        { typeof(ulong), typeof(UnsignedLongType) },
        { typeof(Uri), typeof(UriType) },
        { typeof(ushort), typeof(UnsignedShortType) }
    };

    private static readonly Dictionary<string, Type> s_nameLookup = new()
    {
        { ScalarNames.Any, typeof(AnyType) },
        { ScalarNames.Base64String, typeof(Base64StringType) },
        { ScalarNames.Boolean, typeof(BooleanType) },
        { ScalarNames.Byte, typeof(ByteType) },
#pragma warning disable CS0618 // Type or member is obsolete
        { ScalarNames.ByteArray, typeof(ByteArrayType) },
#pragma warning restore CS0618 // Type or member is obsolete
        { ScalarNames.Date, typeof(DateType) },
        { ScalarNames.DateTime, typeof(DateTimeType) },
        { ScalarNames.Decimal, typeof(DecimalType) },
        { ScalarNames.Float, typeof(FloatType) },
        { ScalarNames.ID, typeof(IdType) },
        { ScalarNames.Int, typeof(IntType) },
        { ScalarNames.LocalDate, typeof(LocalDateType) },
        { ScalarNames.LocalDateTime, typeof(LocalDateTimeType) },
        { ScalarNames.LocalTime, typeof(LocalTimeType) },
        { ScalarNames.Long, typeof(LongType) },
        { ScalarNames.Short, typeof(ShortType) },
        { ScalarNames.String, typeof(StringType) },
        { ScalarNames.TimeSpan, typeof(TimeSpanType) },
        { ScalarNames.UnsignedByte, typeof(UnsignedByteType) },
        { ScalarNames.UnsignedInt, typeof(UnsignedIntType) },
        { ScalarNames.UnsignedLong, typeof(UnsignedLongType) },
        { ScalarNames.UnsignedShort, typeof(UnsignedShortType) },
        { ScalarNames.URI, typeof(UriType) },
        { ScalarNames.URL, typeof(UrlType) },
        { ScalarNames.UUID, typeof(UuidType) }
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

    internal static bool IsSpec(string typeName)
        => s_specScalars.Contains(typeName);
}
