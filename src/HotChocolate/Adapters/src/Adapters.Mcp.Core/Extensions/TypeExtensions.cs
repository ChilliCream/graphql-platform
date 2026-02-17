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
            "https://scalars.graphql.org/chillicream/local-date.html" => Formats.Date,
            "https://scalars.graphql.org/chillicream/time-span.html" => Formats.Duration,
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
            "https://scalars.graphql.org/chillicream/local-date.html"
                => @"^\d{4}-\d{2}-\d{2}$",
            "https://scalars.graphql.org/chillicream/local-date-time.html"
                => @"^\d{4}-\d{2}-\d{2}[Tt]\d{2}:\d{2}:\d{2}(?:\.\d{1,9})?$",
            "https://scalars.graphql.org/chillicream/local-time.html"
                => @"^\d{2}:\d{2}:\d{2}(?:\.\d{1,9})?$",
            "https://scalars.graphql.org/chillicream/time-span.html"
                => @"^-?P(?:\d+W|(?=\d|T(?:\d|$))(?:\d+Y)?(?:\d+M)?(?:\d+D)?(?:T(?:\d+H)?(?:\d+M)?(?:\d+(?:\.\d+)?S)?)?)$",
            "https://scalars.graphql.org/chillicream/uuid.html"
                => @"^[\da-fA-F]{8}-[\da-fA-F]{4}-[\da-fA-F]{4}-[\da-fA-F]{4}-[\da-fA-F]{12}$",
            _ => null
        };

        pattern ??= scalarType.Pattern;
        return pattern is not null;
    }
}
