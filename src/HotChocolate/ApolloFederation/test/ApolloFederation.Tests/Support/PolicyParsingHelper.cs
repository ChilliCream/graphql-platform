using GreenDonut;
using HotChocolate.Language;

namespace HotChocolate.ApolloFederation.Support;

internal static class PolicyParsingHelper
{
    public static string[][] ParseNode(IValueNode node)
    {
        if (node is not ListValueNode list)
        {
            Assert.Fail("Expected ListValueNode result.");
            return null!;
        }

        if (list.Items is not [ListValueNode innerList])
        {
            Assert.Fail("Expected inner ListValueNode result.");
            return null!;
        }

        return innerList.Items
                .OfType<ObjectValueNode>()
                .SelectMany(
                    i => i.Fields,
                    (_, f) => ((StringValueNode)f.Value).Value.Split(","))
                .ToArray();
    }
}
