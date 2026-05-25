using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using HotChocolate.Types;
using Json.Schema;
using static HotChocolate.Adapters.Mcp.Properties.McpAdapterResources;

namespace HotChocolate.Adapters.Mcp.Extensions;

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

        // Minimum.
        if (type.TryGetJsonSchemaMinimum(out var minimum))
        {
            schemaBuilder.Minimum(minimum.Value);
        }

        // Maximum.
        if (type.TryGetJsonSchemaMaximum(out var maximum))
        {
            schemaBuilder.Maximum(maximum.Value);
        }

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
            IScalarTypeDefinition s => GetJsonSchemaValueType(s.GetScalarSerializationType()),
            ListType => SchemaValueType.Array,
            NonNullType => GetJsonSchemaValueType(type.NullableType()),
            _ =>
                throw new NotSupportedException(
                    string.Format(
                        TypeExtensions_UnableToDetermineJsonSchemaValueType,
                        type.GetType().Name))
        };
    }

    private static SchemaValueType GetJsonSchemaValueType(
        ScalarSerializationType scalarSerializationType)
    {
        SchemaValueType result = 0;

        if ((scalarSerializationType & ScalarSerializationType.String) != 0)
        {
            result |= SchemaValueType.String;
        }

        if ((scalarSerializationType & ScalarSerializationType.Boolean) != 0)
        {
            result |= SchemaValueType.Boolean;
        }

        if ((scalarSerializationType & ScalarSerializationType.Int) != 0)
        {
            result |= SchemaValueType.Integer;
        }

        if ((scalarSerializationType & ScalarSerializationType.Float) != 0)
        {
            result |= SchemaValueType.Number;
        }

        if ((scalarSerializationType & ScalarSerializationType.Object) != 0)
        {
            result |= SchemaValueType.Object;
        }

        if ((scalarSerializationType & ScalarSerializationType.List) != 0)
        {
            result |= SchemaValueType.Array;
        }

        // Default to string.
        if (result == 0)
        {
            result = SchemaValueType.String;
        }

        return result;
    }

    private static bool TryGetJsonSchemaMinimum(
        this IType type,
        [NotNullWhen(true)] out decimal? minimum)
    {
        if (type.NullableType() is not IScalarTypeDefinition scalarType)
        {
            minimum = null;
            return false;
        }

        // Built-in scalars.
        if (SpecScalarNames.IsSpecScalar(scalarType.Name))
        {
            minimum = scalarType.Name switch
            {
                // Should be double.MinValue, but JsonSchemaBuilder.Minimum only accepts decimal.
                SpecScalarNames.Float.Name => decimal.MinValue,
                SpecScalarNames.Int.Name => int.MinValue,
                _ => null
            };

            return minimum is not null;
        }

        // Other scalars.
        minimum = scalarType.SpecifiedBy?.OriginalString switch
        {
            "https://scalars.graphql.org/chillicream/byte.html" => sbyte.MinValue,
            "https://scalars.graphql.org/chillicream/long.html" => long.MinValue,
            "https://scalars.graphql.org/chillicream/short.html" => short.MinValue,
            "https://scalars.graphql.org/chillicream/unsigned-byte.html" => byte.MinValue,
            "https://scalars.graphql.org/chillicream/unsigned-int.html" => uint.MinValue,
            "https://scalars.graphql.org/chillicream/unsigned-long.html" => ulong.MinValue,
            "https://scalars.graphql.org/chillicream/unsigned-short.html" => ushort.MinValue,
            _ => null
        };

        return minimum is not null;
    }

    private static bool TryGetJsonSchemaMaximum(
        this IType type,
        [NotNullWhen(true)] out decimal? maximum)
    {
        if (type.NullableType() is not IScalarTypeDefinition scalarType)
        {
            maximum = null;
            return false;
        }

        // Built-in scalars.
        if (SpecScalarNames.IsSpecScalar(scalarType.Name))
        {
            maximum = scalarType.Name switch
            {
                // Should be double.MaxValue, but JsonSchemaBuilder.Maximum only accepts decimal.
                SpecScalarNames.Float.Name => decimal.MaxValue,
                SpecScalarNames.Int.Name => int.MaxValue,
                _ => null
            };

            return maximum is not null;
        }

        // Other scalars.
        maximum = scalarType.SpecifiedBy?.OriginalString switch
        {
            "https://scalars.graphql.org/chillicream/byte.html" => sbyte.MaxValue,
            "https://scalars.graphql.org/chillicream/long.html" => long.MaxValue,
            "https://scalars.graphql.org/chillicream/short.html" => short.MaxValue,
            "https://scalars.graphql.org/chillicream/unsigned-byte.html" => byte.MaxValue,
            "https://scalars.graphql.org/chillicream/unsigned-int.html" => uint.MaxValue,
            "https://scalars.graphql.org/chillicream/unsigned-long.html" => ulong.MaxValue,
            "https://scalars.graphql.org/chillicream/unsigned-short.html" => ushort.MaxValue,
            _ => null
        };

        return maximum is not null;
    }

    private static bool TryGetJsonSchemaFormat(
        this IType type,
        [NotNullWhen(true)] out Format? format)
    {
        if (type.NullableType() is not IScalarTypeDefinition scalarType)
        {
            format = null;
            return false;
        }

        format = scalarType.SpecifiedBy?.OriginalString switch
        {
            "https://scalars.graphql.org/chillicream/date.html" => Formats.Date,
            "https://scalars.graphql.org/chillicream/date-time.html" => Formats.DateTime,
            "https://scalars.graphql.org/chillicream/duration.html" => Formats.Duration,
            "https://scalars.graphql.org/chillicream/local-date.html" => Formats.Date,
            "https://scalars.graphql.org/chillicream/uuid.html" => Formats.Uuid,
            "https://scalars.graphql.org/chillicream/uri.html" => Formats.Uri,
            "https://scalars.graphql.org/chillicream/url.html" => Formats.Uri,
            _ => null
        };

        return format is not null;
    }

    private static bool TryGetJsonSchemaPattern(
        this IType type,
        [NotNullWhen(true)] out string? pattern)
    {
        if (type.NullableType() is not IScalarTypeDefinition scalarType)
        {
            pattern = null;
            return false;
        }

        pattern = scalarType.SpecifiedBy?.OriginalString switch
        {
            "https://scalars.graphql.org/chillicream/base64-string.html"
                => @"^(?:[A-Za-z0-9+\/]{4})*(?:[A-Za-z0-9+\/]{2}==|[A-Za-z0-9+\/]{3}=)?$",
            "https://scalars.graphql.org/chillicream/date.html"
                => @"^\d{4}-\d{2}-\d{2}$",
            "https://scalars.graphql.org/chillicream/date-time.html"
                => @"^\d{4}-\d{2}-\d{2}[Tt]\d{2}:\d{2}:\d{2}(?:\.\d{1,9})?(?:[Zz]|[+-]\d{2}:\d{2})$",
            "https://scalars.graphql.org/chillicream/duration.html"
                => @"^-?P(?:\d+W|(?=\d|T(?:\d|$))(?:\d+Y)?(?:\d+M)?(?:\d+D)?(?:T(?:\d+H)?(?:\d+M)?(?:\d+(?:\.\d+)?S)?)?)$",
            "https://scalars.graphql.org/chillicream/local-date.html"
                => @"^\d{4}-\d{2}-\d{2}$",
            "https://scalars.graphql.org/chillicream/local-date-time.html"
                => @"^\d{4}-\d{2}-\d{2}[Tt]\d{2}:\d{2}:\d{2}(?:\.\d{1,9})?$",
            "https://scalars.graphql.org/chillicream/local-time.html"
                => @"^\d{2}:\d{2}:\d{2}(?:\.\d{1,9})?$",
            "https://scalars.graphql.org/chillicream/uuid.html"
                => @"^[\da-fA-F]{8}-[\da-fA-F]{4}-[\da-fA-F]{4}-[\da-fA-F]{4}-[\da-fA-F]{12}$",
            _ => null
        };

        pattern ??= scalarType.Pattern;
        return pattern is not null;
    }
}
