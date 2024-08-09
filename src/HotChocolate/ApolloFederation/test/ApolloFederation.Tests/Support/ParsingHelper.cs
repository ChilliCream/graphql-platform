using HotChocolate.Language;

namespace HotChocolate.ApolloFederation.Support;

internal static class ParsingHelper
{
    public static string[][] ParsePolicyDirectiveNode(IValueNode node)
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
            .OfType<StringValueNode>()
            .Select(t => t.Value.Split(',').Select(a => a.Trim()).ToArray())
            .ToArray();
    }

    public static string[][] ParseRequiresScopesDirectiveNode(IValueNode node)
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
            .OfType<StringValueNode>()
            .Select(t => t.Value.Split(',').Select(a => a.Trim()).ToArray())
            .ToArray();
    }
}
