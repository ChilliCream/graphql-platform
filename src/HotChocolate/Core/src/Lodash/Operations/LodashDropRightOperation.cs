using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json.Nodes;
using HotChocolate.Language;

namespace HotChocolate.Lodash
{
    public class LodashDropRightOperation : LodashOperation
    {
        public LodashDropRightOperation(int count)
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

                int count;
                int i;
                for (count = Count - 1, i = arr.Count - 1; count >= 0; count--, i--)
                {
                    arr.RemoveAt(i);
                }

                return arr;
            }

            throw ThrowHelper.ExpectArrayButReceivedObject(node.GetPath());
        }

        public static readonly ILodashOperationFactory Factory =
            new LodashDropRightOperationFactory();

        private class LodashDropRightOperationFactory : ILodashOperationFactory
        {
            public bool TryCreateOperation(
                DirectiveNode directiveNode,
                [NotNullWhen(true)] out LodashOperation? operation)
            {
                ArgumentNode? map =
                    directiveNode.Arguments.FirstOrDefault(x => x.Name.Value == "dropRight");

                if (map is not null && map.Value is IntValueNode intValueNode)
                {
                    operation = new LodashDropRightOperation(intValueNode.ToInt32());
                    return true;
                }

                operation = null;
                return false;
            }
        }
    }
}
