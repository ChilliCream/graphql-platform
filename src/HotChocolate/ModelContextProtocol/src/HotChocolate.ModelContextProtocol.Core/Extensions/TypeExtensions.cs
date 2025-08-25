using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using HotChocolate.Types;
using Json.Schema;
using static HotChocolate.ModelContextProtocol.Properties.ModelContextProtocolResources;

namespace HotChocolate.ModelContextProtocol.Extensions;

internal static class TypeExtensions
{
    public static JsonSchemaBuilder ToJsonSchemaBuilder(this IType type, bool isOneOf = false)
    {
        var schemaBuilder = new JsonSchemaBuilder();

        // Type.
        var jsonType = type.GetJsonSchemaValueType();

        if (!type.IsNonNullType() && !isOneOf)
        {
            // Nullability.
            jsonType |= SchemaValueType.Null;
        }

        schemaBuilder.Type(jsonType);

        // Format.
        if (type.TryGetJsonSchemaFormat(out var format))
        {
            schemaBuilder.Format(format);
        }

        // Pattern.
        if (type.TryGetJsonSchemaPattern(out var pattern))
        {
            schemaBuilder.Pattern(pattern);
        }

        // Minimum.
        if (type.TryGetJsonSchemaMinimum(out var minimum))
        {
            schemaBuilder.Minimum(minimum);
        }

        // Maximum.
        if (type.TryGetJsonSchemaMaximum(out var maximum))
        {
            schemaBuilder.Maximum(maximum);
        }

        switch (type.NullableType())
        {
            case IEnumTypeDefinition enumType:
                // Enum values.
                List<JsonValue?> enumValues = [];

                foreach (var enumValue in enumType.Values)
                {
                    enumValues.Add(JsonValue.Create(enumValue.Name));
                }

                if (type.IsNullableType())
                {
                    enumValues.Add(null);
                }

                schemaBuilder.Enum(enumValues);
                break;

            case IInputObjectTypeDefinition inputObjectType:
                // Object properties.
                var objectProperties = new Dictionary<string, JsonSchema>();
                var requiredObjectProperties = new List<string>();

                foreach (var field in inputObjectType.Fields)
                {
                    var fieldSchema = field.ToJsonSchema();

                    objectProperties.Add(field.Name, fieldSchema);

                    if (field.Type.IsNonNullType() && field.DefaultValue is null)
                    {
                        requiredObjectProperties.Add(field.Name);
                    }
                }

                // OneOf.
                if (inputObjectType.IsOneOf)
                {
                    List<JsonSchema> oneOfSchemas = [];

                    foreach (var (propertyName, propertySchema) in objectProperties)
                    {
                        var oneOfSchema = new JsonSchemaBuilder();

                        oneOfSchema
                            .Type(SchemaValueType.Object)
                            .Properties((propertyName, propertySchema))
                            .Required(propertyName);

                        oneOfSchemas.Add(oneOfSchema.Build());
                    }

                    schemaBuilder.OneOf(oneOfSchemas);
                }
                else
                {
                    schemaBuilder.Properties(objectProperties);
                    schemaBuilder.Required(requiredObjectProperties);
                }

                break;

            case ListType listType:
                // Array items.
                schemaBuilder.Items(listType.ElementType().ToJsonSchemaBuilder());
                break;
        }

        return schemaBuilder;
    }

    private static SchemaValueType GetJsonSchemaValueType(this IType type)
    {
        return type switch
        {
            IEnumTypeDefinition => SchemaValueType.String,
            IInputObjectTypeDefinition or IInterfaceTypeDefinition or IObjectTypeDefinition or IUnionTypeDefinition
                => SchemaValueType.Object,
            ListType => SchemaValueType.Array,
            NonNullType => GetJsonSchemaValueType(type.NullableType()),
            IScalarTypeDefinition => type switch
            {
                AnyType or JsonType =>
                    SchemaValueType.Object
                    | SchemaValueType.Array
                    | SchemaValueType.Boolean
                    | SchemaValueType.String
                    | SchemaValueType.Number
                    | SchemaValueType.Integer,
                BooleanType => SchemaValueType.Boolean,
                ByteType or IntType or LongType or ShortType => SchemaValueType.Integer,
                DecimalType or FloatType => SchemaValueType.Number,
                IdType or StringType => SchemaValueType.String,
                // The following types are serialized as strings:
                // URL, UUID, ByteArray, DateTime, Date, TimeSpan, LocalDate, LocalDateTime,
                // LocalTime.
                // TODO: Treating all unknown scalar types as strings is a temporary solution.
                _ => SchemaValueType.String
            },
            _ =>
                throw new NotSupportedException(
                    string.Format(
                        TypeExtensions_UnableToDetermineJsonSchemaValueType,
                        type.GetType().Name))
        };
    }

    private static bool TryGetJsonSchemaFormat(
        this IType type,
        [NotNullWhen(true)] out Format? format)
    {
        format = type.NullableType() switch
        {
            DateTimeType => Formats.DateTime,      // Further constrained by pattern.
            DateType => Formats.Date,
            LocalDateTimeType => Formats.DateTime, // Further constrained by pattern.
            LocalDateType => Formats.Date,
            LocalTimeType => Formats.Time,         // Further constrained by pattern.
            UrlType => Formats.UriReference,
            UuidType => Formats.Uuid,
            _ => null
        };

        return format is not null;
    }

    private static bool TryGetJsonSchemaPattern(
        this IType type,
        [NotNullWhen(true)] out string? pattern)
    {
        pattern = type.NullableType() switch
        {
            ByteArrayType
                // e.g. dmFsdWU= (Base64-encoded string)
                => @"^(?:[A-Za-z0-9+\/]{4})*(?:[A-Za-z0-9+\/]{2}==|[A-Za-z0-9+\/]{3}=)?$",
            DateTimeType
                // e.g. 2011-08-30T13:22:53.108Z (https://www.graphql-scalars.com/date-time/)
                =>
                    @"^\d{4}-\d{2}-\d{2}[Tt]\d{2}:\d{2}:\d{2}"
                    + @"(?:\.\d{1,7})?(?:[Zz]|[+-]\d{2}:\d{2})$",
            LocalDateTimeType
                // e.g. 2011-08-30T13:22:53
                => @"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}$",
            LocalTimeType
                // e.g. 13:22:53
                => @"^\d{2}:\d{2}:\d{2}$",
            TimeSpanType timeSpanType
                => timeSpanType.Format switch
                {
                    // e.g. PT5M
                    TimeSpanFormat.Iso8601
                        =>
                            @"^-?P(?:\d+W|(?=\d|T(?:\d|$))(?:\d+Y)?(?:\d+M)?(?:\d+D)?"
                            + @"(?:T(?:\d+H)?(?:\d+M)?(?:\d+(?:\.\d+)?S)?)?)$",
                    // e.g. 00:05:00
                    TimeSpanFormat.DotNet
                        =>
                            @"^-?(?:(?:\d{1,8})\.)?(?:[0-1]?\d|2[0-3]):(?:[0-5]?\d):(?:[0-5]?\d)"
                            + @"(?:\.(?:\d{1,7}))?$",
                    _ => throw new InvalidOperationException()
                },
            _ => null
        };

        return pattern is not null;
    }

    private static bool TryGetJsonSchemaMinimum(this IType type, out decimal minimum)
    {
        switch (type.NullableType())
        {
            case ByteType byteType when byteType.MinValue != byte.MinValue:
                minimum = byteType.MinValue;
                return true;

            case DecimalType decimalType when decimalType.MinValue != decimal.MinValue:
                minimum = decimalType.MinValue;
                return true;

            case FloatType { MinValue: >= (double)decimal.MinValue } floatType:
                minimum = (decimal)floatType.MinValue;
                return true;

            case IntType intType when intType.MinValue != int.MinValue:
                minimum = intType.MinValue;
                return true;

            case LongType longType when longType.MinValue != long.MinValue:
                minimum = longType.MinValue;
                return true;

            case ShortType shortType when shortType.MinValue != short.MinValue:
                minimum = shortType.MinValue;
                return true;
        }

        minimum = 0;
        return false;
    }

    private static bool TryGetJsonSchemaMaximum(this IType type, out decimal maximum)
    {
        switch (type.NullableType())
        {
            case ByteType byteType when byteType.MaxValue != byte.MaxValue:
                maximum = byteType.MaxValue;
                return true;

            case DecimalType decimalType when decimalType.MaxValue != decimal.MaxValue:
                maximum = decimalType.MaxValue;
                return true;

            case FloatType { MaxValue: <= (double)decimal.MaxValue } floatType:
                maximum = (decimal)floatType.MaxValue;
                return true;

            case IntType intType when intType.MaxValue != int.MaxValue:
                maximum = intType.MaxValue;
                return true;

            case LongType longType when longType.MaxValue != long.MaxValue:
                maximum = longType.MaxValue;
                return true;

            case ShortType shortType when shortType.MaxValue != short.MaxValue:
                maximum = shortType.MaxValue;
                return true;
        }

        maximum = 0;
        return false;
    }
}
