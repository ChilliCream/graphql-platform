using System.Text.Json;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Utilities;

internal static class JsonValueToGraphQLValueConverter
{
    public static IValueNode Convert(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var fields = new List<ObjectFieldNode>();

                foreach (var property in element.EnumerateObject())
                {
                    fields.Add(new ObjectFieldNode(property.Name, Convert(property.Value)));
                }

                return new ObjectValueNode(fields);

            case JsonValueKind.Array:
                var index = 0;
                var items = new IValueNode[element.GetArrayLength()];

                foreach (var item in element.EnumerateArray())
                {
                    items[index++] = Convert(item);
                }

                return new ListValueNode(items);

            case JsonValueKind.String:
                return new StringValueNode(element.GetString()!);

            case JsonValueKind.Number:
                return Utf8GraphQLParser.Syntax.ParseValueLiteral(element.GetRawText());

            case JsonValueKind.True:
                return BooleanValueNode.True;

            case JsonValueKind.False:
                return BooleanValueNode.False;

            case JsonValueKind.Null:
                return NullValueNode.Default;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
