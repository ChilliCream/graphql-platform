using HotChocolate.ApolloFederation.Constants;
using HotChocolate.Language;

namespace HotChocolate.ApolloFederation;

/// <summary>
/// </summary>
public sealed class PolicyCollectionType : ScalarType<PolicyCollection, ListValueNode>
{
    public PolicyCollectionType(BindingBehavior bind = BindingBehavior.Explicit)
        : base(WellKnownTypeNames.PolicyDirective, bind)
    {
    }

    public override IValueNode ParseResult(object? resultValue)
    {
        if (resultValue is null)
        {
            return NullValueNode.Default;
        }
        if (resultValue is PolicyCollection policyCollection)
        {
            return ParseValue(policyCollection);
        }
        throw new Exception("object of unexpected type");
    }

    protected override PolicyCollection ParseLiteral(ListValueNode valueSyntax)
    {
        var result = PolicyParsingHelper.ParseNode(valueSyntax);
        return result;
    }

    protected override ListValueNode ParseValue(PolicyCollection runtimeValue)
    {
        var policySets = runtimeValue.PolicySets;
        var policySetCount = policySets.Length;

        var policySetNodes = new IValueNode[policySetCount];
        for (var policySetIndex = 0; policySetIndex < policySetCount; policySetIndex++)
        {
            var policySet = policySets[policySetIndex];
            var policies = policySet.Policies;
            var policyCount = policies.Length;

            var policyNameNodes = new IValueNode[policyCount];
            for (var policyIndex = 0; policyIndex < policyCount; policyIndex++)
            {
                var policyName = policies[policyIndex].Name;
                policyNameNodes[policyIndex] = new StringValueNode(policyName);
            }
            policySetNodes[policySetIndex] = new ListValueNode(policyNameNodes);
        }
        var result = new ListValueNode(policySetNodes);

        return result;
    }
}

public static class PolicyParsingHelper
{
    public static PolicyCollection ParseNode(ListValueNode node)
    {
        var policySetNodes = node.Items;
        var policySetCount = policySetNodes.Count;

        var policySets = new PolicySet[policySetCount];
        for (var policySetIndex = 0; policySetIndex < policySetCount; policySetIndex++)
        {
            var item = policySetNodes[policySetIndex];
            if (item is not ListValueNode policySetNode)
            {
                throw new Exception("Expected list of strings");
            }

            var policyNameNodes = policySetNode.Items;
            var policyNameCount = policyNameNodes.Count;
            var policies = new Policy[policyNameCount];
            for (var policyNameIndex = 0; policyNameIndex < policyNameCount; policyNameIndex++)
            {
                var policyNameNode = policyNameNodes[policyNameIndex];
                if (policyNameNode is not StringValueNode stringPolicyNameNode)
                {
                    throw new Exception("Expected string");
                }
                policies[policyNameIndex] = new Policy
                {
                    Name = stringPolicyNameNode.Value,
                };
            }

            policySets[policySetIndex] = new PolicySet
            {
                Policies = policies,
            };
        }
        var result = new PolicyCollection
        {
            PolicySets = policySets,
        };

        return result;
    }

}

public struct Policy
{
    public required string Name { get; init; }
}

/// <summary>
/// Represents a set of multiple policies.
/// </summary>
public struct PolicySet
{
    /// <summary>
    /// Includes policies included in this set.
    /// </summary>
    public required Policy[] Policies { get; init; }
}

public sealed class PolicyCollection
{
    /// <summary>
    /// Either of the policy sets listed here must be satisfied.
    /// </summary>
    public required PolicySet[] PolicySets { get; init; }

    public static PolicyCollection FromNameSets(string[][] names)
    {
        var policySets = new PolicySet[names.Length];
        int policySetCount = names.Length;
        for (var policySetIndex = 0; policySetIndex < policySetCount; policySetIndex++)
        {
            var policyNames = names[policySetIndex];
            var policyCount = policyNames.Length;
            var policies = new Policy[policyCount];
            for (var policyIndex = 0; policyIndex < policyCount; policyIndex++)
            {
                policies[policyIndex] = new Policy
                {
                    Name = policyNames[policyIndex],
                };
            }
            policySets[policySetIndex] = new PolicySet
            {
                Policies = policies,
            };
        }

        return new()
        {
            PolicySets = policySets,
        };
    }
}
