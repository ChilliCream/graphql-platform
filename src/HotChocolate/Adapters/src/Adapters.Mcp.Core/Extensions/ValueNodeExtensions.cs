using System.Text.Json.Nodes;
using HotChocolate.Language;
using HotChocolate.Types;
using static HotChocolate.Adapters.Mcp.Properties.McpAdapterResources;

namespace HotChocolate.Adapters.Mcp.Extensions;

internal static class ValueNodeExtensions
{
    public static JsonNode? ToJsonNode(this IValueNode valueNode, IType type)
    {
        var nullableType = type.NullableType();

        return valueNode switch
        {
            BooleanValueNode booleanValueNode => JsonValue.Create(booleanValueNode.Value),
            EnumValueNode enumValueNode => JsonValue.Create(enumValueNode.Value),
            FloatValueNode floatValueNode when nullableType is IScalarTypeDefinition scalarType
                => scalarType.GetScalarSerializationType() switch
                {
                    ScalarSerializationType.Float => JsonValue.Create(floatValueNode.ToDecimal()),
                    _ => JsonValue.Create(floatValueNode.Value)
                },
            IntValueNode intValueNode when nullableType is IScalarTypeDefinition scalarType
                => scalarType.GetScalarSerializationType() switch
                {
                    ScalarSerializationType.Float => JsonValue.Create(intValueNode.ToDecimal()),
                    ScalarSerializationType.Int => JsonValue.Create(intValueNode.ToInt64()),
                    _ => JsonValue.Create(intValueNode.Value)
                },
            ListValueNode listValueNode => listValueNode.ToJsonNode(nullableType),
            NullValueNode => null,
            ObjectValueNode objectValueNode => objectValueNode.ToJsonNode(nullableType),
            StringValueNode stringValueNode => JsonValue.Create(stringValueNode.Value),
            _ =>
                throw new NotSupportedException(
                    string.Format(
                        ValueNodeExtensions_UnableToConvertValueNodeToJsonNode,
                        valueNode.GetType().Name))
        };
    }

    private static JsonArray ToJsonNode(this ListValueNode valueNode, IType listType)
    {
        var jsonArray = new JsonArray();

        foreach (var item in valueNode.Items)
        {
            jsonArray.Add(item.ToJsonNode(listType.ElementType()));
        }

        return jsonArray;
    }

    private static JsonObject ToJsonNode(this ObjectValueNode valueNode, IType objectType)
    {
        var jsonObject = new JsonObject();

        foreach (var field in valueNode.Fields)
        {
            var graphQLFieldType = objectType is IInputObjectTypeDefinition inputObjectType
                ? inputObjectType.Fields[field.Name.Value].Type
                // Placeholder for fields of scalar types like JsonType or AnyType.
                : new MissingType("");

            jsonObject.Add(field.Name.Value, field.Value.ToJsonNode(graphQLFieldType));
        }

        return jsonObject;
    }
}
