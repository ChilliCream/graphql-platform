using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

#nullable enable

namespace HotChocolate.Types;

/// <summary>
/// This class provides helper methods to deal with scalar types.
/// </summary>
public static class Scalars
{
    private static readonly Dictionary<Type, Type> _lookup = new()
    {
        { typeof(string), typeof(StringType) },
        { typeof(bool), typeof(BooleanType) },
        { typeof(byte), typeof(ByteType) },
        { typeof(short), typeof(ShortType) },
        { typeof(int), typeof(IntType) },
        { typeof(long), typeof(LongType) },

        { typeof(float), typeof(FloatType) },
        { typeof(double), typeof(FloatType) },
        { typeof(decimal), typeof(DecimalType) },

        { typeof(Uri), typeof(UrlType) },
        { typeof(Guid), typeof(UuidType) },
        { typeof(DateTime), typeof(DateTimeType) },
        { typeof(DateTimeOffset), typeof(DateTimeType) },
        { typeof(byte[]), typeof(ByteArrayType) },
        { typeof(TimeSpan), typeof(TimeSpanType) },

        { typeof(DateOnly), typeof(DateType) },
        { typeof(TimeOnly), typeof(TimeSpanType) },
        { typeof(JsonElement), typeof(JsonType) },
    };

    private static readonly Dictionary<string, Type> _nameLookup = new()
    {
        { ScalarNames.String, typeof(StringType) },
        { ScalarNames.ID, typeof(IdType) },
        { ScalarNames.Boolean, typeof(BooleanType) },
        { ScalarNames.Byte, typeof(ByteType) },
        { ScalarNames.Short, typeof(ShortType) },
        { ScalarNames.Int, typeof(IntType) },
        { ScalarNames.Long, typeof(LongType) },

        { ScalarNames.Float, typeof(FloatType) },
        { ScalarNames.Decimal, typeof(DecimalType) },

        { ScalarNames.URL, typeof(UrlType) },
        { ScalarNames.UUID, typeof(UuidType) },
        { ScalarNames.DateTime, typeof(DateTimeType) },
        { ScalarNames.Date, typeof(DateType) },
        { ScalarNames.TimeSpan, typeof(TimeSpanType) },
        { ScalarNames.Any, typeof(AnyType) },

        { ScalarNames.ByteArray, typeof(ByteArrayType) },
        { ScalarNames.JSON, typeof(JsonType) }
    };

    private static readonly Dictionary<Type, ValueKind> _scalarKinds = new()
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
        { typeof(bool?), ValueKind.Float },
    };

    internal static bool TryGetScalar(
        Type runtimeType,
        [NotNullWhen(true)] out Type? schemaType) =>
        _lookup.TryGetValue(
            runtimeType ?? throw new ArgumentNullException(nameof(runtimeType)),
            out schemaType);

    internal static bool TryGetScalar(
        string typeName,
        [NotNullWhen(true)] out Type? schemaType)
    {
        if (string.IsNullOrEmpty(typeName))
        {
            throw new ArgumentNullException(nameof(typeName));
        }

        return _nameLookup.TryGetValue(typeName, out schemaType);
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
        => !string.IsNullOrEmpty(typeName) &&
            _nameLookup.ContainsKey(typeName);

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

        if (_scalarKinds.TryGetValue(valueType, out kind))
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
}
