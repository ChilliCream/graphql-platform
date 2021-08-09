using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json.Nodes;
using HotChocolate.Language;

namespace HotChocolate.Lodash
{
    public class LodashTakeOperation : LodashOperation
    {
        public LodashTakeOperation(int count)
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
                var initialCount = arr.Count;
                var newArray = new JsonArray();
                for (var count = 0; count < Count && count < initialCount; count++)
                {
                    JsonNode? element = arr[0];
                    arr.RemoveAt(0);
                    newArray.Add(element);
                }

                return newArray;
            }

            throw ThrowHelper.ExpectArrayButReceivedObject(node.GetPath());
        }

        public static readonly ILodashOperationFactory Factory =
            new LodashTakeOperationFactory();

        private class LodashTakeOperationFactory : ILodashOperationFactory
        {
            public bool TryCreateOperation(
                DirectiveNode directiveNode,
                [NotNullWhen(true)] out LodashOperation? operation)
            {
                ArgumentNode? map =
                    directiveNode.Arguments.FirstOrDefault(x => x.Name.Value == "take");

                if (map is not null && map.Value is IntValueNode intValueNode)
                {
                    operation = new LodashTakeOperation(intValueNode.ToInt32());
                    return true;
                }

                operation = null;
                return false;
            }
        }
    }
}
