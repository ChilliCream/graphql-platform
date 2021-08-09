using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json.Nodes;
using HotChocolate.Language;

namespace HotChocolate.Lodash
{
    public class LodashTakeRightOperation : LodashOperation
    {
        public LodashTakeRightOperation(int count)
        {
            Count = count;
        }

        public int Count { get; }

        public override JsonNode? Rewrite(JsonNode? node)
        {
            if (node is null)
            {
                return null;
            }

            if (node is JsonArray arr)
            {
                JsonArray newArray = new();
                int count;
                int i;
                for (count = Count - 1, i = arr.Count - 1; count >= 0 && i >= 0; count--, i--)
                {
                    JsonNode? element = arr[i];
                    arr.RemoveAt(i);
                    newArray.Add(element);
                }

                return newArray;
            }

            throw ThrowHelper.ExpectArrayButReceivedObject(node.GetPath());
        }

        public static readonly ILodashOperationFactory Factory =
            new LodashTakeRightOperationFactory();

        private class LodashTakeRightOperationFactory : ILodashOperationFactory
        {
            public bool TryCreateOperation(
                DirectiveNode directiveNode,
                [NotNullWhen(true)] out LodashOperation? operation)
            {
                ArgumentNode? map =
                    directiveNode.Arguments.FirstOrDefault(x => x.Name.Value == "takeRight");

                if (map is not null && map.Value is IntValueNode intValueNode)
                {
                    operation = new LodashTakeRightOperation(intValueNode.ToInt32());
                    return true;
                }

                operation = null;
                return false;
            }
        }
    }
}
