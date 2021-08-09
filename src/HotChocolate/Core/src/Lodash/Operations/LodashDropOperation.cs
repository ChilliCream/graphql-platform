using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json.Nodes;
using HotChocolate.Language;

namespace HotChocolate.Lodash
{
    public class LodashDropOperation : LodashOperation
    {
        public LodashDropOperation(int count)
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
                if (Count > arr.Count)
                {
                    return new JsonArray();
                }

                for (var count = Count - 1; count >= 0; count--)
                {
                    arr.RemoveAt(0);
                }

                return arr;
            }

            throw ThrowHelper.ExpectArrayButReceivedObject(node.GetPath());
        }

        public static readonly ILodashOperationFactory Factory =
            new LodashDropOperationFactory();

        private class LodashDropOperationFactory : ILodashOperationFactory
        {
            public bool TryCreateOperation(
                DirectiveNode directiveNode,
                [NotNullWhen(true)] out LodashOperation? operation)
            {
                ArgumentNode? map =
                    directiveNode.Arguments.FirstOrDefault(x => x.Name.Value == "drop");

                if (map is not null && map.Value is IntValueNode intValueNode)
                {
                    operation = new LodashDropOperation(intValueNode.ToInt32());
                    return true;
                }

                operation = null;
                return false;
            }
        }
    }
}
