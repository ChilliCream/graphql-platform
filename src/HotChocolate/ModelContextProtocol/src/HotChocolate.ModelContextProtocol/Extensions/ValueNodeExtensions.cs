using System.Text.Json.Nodes;
using HotChocolate.Language;
using HotChocolate.Types;
using static HotChocolate.ModelContextProtocol.Properties.ModelContextProtocolResources;

namespace HotChocolate.ModelContextProtocol.Extensions;

internal static class ValueNodeExtensions
{
    public static JsonNode? ToJsonNode(this IValueNode valueNode, IType graphQLType)
    {
        var nullableType = graphQLType.NullableType();

        return valueNode switch
        {
            BooleanValueNode booleanValueNode => JsonValue.Create(booleanValueNode.Value),
            EnumValueNode enumValueNode => JsonValue.Create(enumValueNode.Value),
            FloatValueNode floatValueNode => nullableType switch
            {
                DecimalType => JsonValue.Create(floatValueNode.ToDecimal()),
                FloatType => JsonValue.Create(floatValueNode.ToDouble()),
                // TODO: Treating all unknown scalar types as strings is a temporary solution.
                _ => JsonValue.Create(floatValueNode.Value)
            },
            IntValueNode intValueNode => nullableType switch
            {
                ByteType => JsonValue.Create(intValueNode.ToByte()),
                DecimalType => JsonValue.Create(intValueNode.ToDecimal()),
                FloatType => JsonValue.Create(intValueNode.ToDouble()),
                IntType => JsonValue.Create(intValueNode.ToInt32()),
                LongType => JsonValue.Create(intValueNode.ToInt64()),
                ShortType => JsonValue.Create(intValueNode.ToInt16()),
                // TODO: Treating all unknown scalar types as strings is a temporary solution.
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
            var graphQLFieldType = objectType is InputObjectType inputObjectType
                ? inputObjectType.Fields[field.Name.Value].Type
                : new AnyType(); // Types like JsonType or AnyType have no schema.

            jsonObject.Add(field.Name.Value, field.Value.ToJsonNode(graphQLFieldType));
        }

        return jsonObject;
    }
}
