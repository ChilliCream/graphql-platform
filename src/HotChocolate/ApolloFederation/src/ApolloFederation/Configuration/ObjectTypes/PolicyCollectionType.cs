using HotChocolate.ApolloFederation.Constants;
using HotChocolate.ApolloFederation.Properties;
using HotChocolate.Language;

namespace HotChocolate.ApolloFederation;

/// <summary>
/// </summary>
public sealed class PolicyCollectionType : ScalarType<string[][]>
{
    public PolicyCollectionType(BindingBehavior bind = BindingBehavior.Explicit)
        : base(WellKnownTypeNames.PolicyDirective, bind)
    {
    }

    public override bool IsInstanceOfType(IValueNode valueSyntax)
        => PolicyParsingHelper.CanParseNode(valueSyntax);

    public override object ParseLiteral(IValueNode valueSyntax)
        => PolicyParsingHelper.ParseNode(valueSyntax);

    public override IValueNode ParseValue(object? runtimeValue)
    {
        if (runtimeValue is not string[][] policies1)
        {
            throw new ArgumentException(
                FederationResources.PolicyCollectionType_ParseValue_ExpectedStringArray,
                nameof(runtimeValue));
        }

        var list1 = new IValueNode[policies1.Length];
        for (int i1 = 0; i1 < list1.Length; i1++)
        {
            var policies2 = policies1[i1];
            var list2 = new IValueNode[policies2.Length];
            for (int i2 = 0; i2 < list2.Length; i2++)
            {
                list2[i2] = new StringValueNode(policies2[i2]);
            }

            list1[i1] = new ListValueNode(list2);
        }

        var result = new ListValueNode(list1);
        return result;
    }

    public override IValueNode ParseResult(object? resultValue)
        => ParseValue(resultValue);
}


public static class PolicyParsingHelper
{
    private static bool IsNestedList(
        IValueNode syntaxNode,
        int numDimensions)
    {
        if (syntaxNode.Kind == SyntaxKind.StringValue)
        {
            return true;
        }

        if (numDimensions == 0)
        {
            return false;
        }

        if (syntaxNode is not ListValueNode list)
        {
            return false;
        }

        foreach (var item in list.Items)
        {
            if (!IsNestedList(item, numDimensions - 1))
            {
                return false;
            }
        }
        return true;
    }

    private static string[] ParseNestedList1(IValueNode syntaxNode)
    {
        if (syntaxNode.Kind == SyntaxKind.StringValue)
        {
            return [(string)syntaxNode.Value!];
        }

        var listNode = (ListValueNode)syntaxNode;
        var items = listNode.Items;
        var array = new string[items.Count];
        for (var i = 0; i < array.Length; i++)
        {
            array[i] = (string)items[i].Value!;
        }
        return array;
    }

    private static string[][] ParseNestedList2(IValueNode syntaxNode)
    {
        if (syntaxNode.Kind == SyntaxKind.StringValue)
        {
            return [[(string)syntaxNode.Value!]];
        }

        var listNode = (ListValueNode)syntaxNode;
        var items = listNode.Items;
        var array = new string[][items.Count];
        for (var i = 0; i < array.Length; i++)
        {
            array[i] = ParseNestedList1(items[i]);
        }
        return array;
    }

    public static string[][] ParseNode(IValueNode syntaxNode)
        => ParseNestedList2(syntaxNode);
    public static bool CanParseNode(IValueNode syntaxNode)
        => IsNestedList(syntaxNode, numDimensions: 2);
}
