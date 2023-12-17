using System.Collections.Generic;
using HotChocolate.ApolloFederation.Constants;
using HotChocolate.ApolloFederation.Properties;
using HotChocolate.Language;

namespace HotChocolate.ApolloFederation;

/// <summary>
/// A union called _Entity which is a union of all types that use the @key directive,
/// including both types native to the schema and extended types.
/// </summary>
public sealed class PolicyType : ScalarType<PolicyCollection, ListValueNode>
{
    public PolicyType(string name, BindingBehavior bind = BindingBehavior.Explicit) : base(name, bind)
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
        var policySetNodes = valueSyntax.Items;
        int policySetCount = policySetNodes.Count;

        var policySets = new PolicySet[policySetCount];
        for (int policySetIndex = 0; policySetIndex < policySetCount; policySetIndex++)
        {
            var item = policySetNodes[policySetIndex];
            if (item is not ListValueNode policySetNode)
            {
                throw new Exception("Expected list of strings");
            }

            var policyNameNodes = policySetNode.Items;
            int policyNameCount = policyNameNodes.Count;
            var policies = new Policy[policyNameCount];
            for (int policyNameIndex = 0; policyNameIndex < policyNameCount; policyNameIndex++)
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
                PolicyNames = policies,
            };
        }
        var result = new PolicyCollection
        {
            PolicySets = policySets,
        };

        return result;
    }

    protected override ListValueNode ParseValue(PolicyCollection runtimeValue)
    {
        var policySets = runtimeValue.PolicySets;
        var policySetCount = policySets.Length;

        var policySetNodes = new IValueNode[policySetCount];
        for (int policySetIndex = 0; policySetIndex < policySetCount; policySetIndex++)
        {
            var policySet = policySets[policySetIndex];
            var policies = policySet.PolicyNames;
            var policyCount = policies.Length;

            var policyNameNodes = new IValueNode[policyCount];
            for (int policyIndex = 0; policyIndex < policyCount; policyIndex++)
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
    /// Includes names of policies included in this set.
    /// </summary>
    public required Policy[] PolicyNames { get; init; }
}

public sealed class PolicyCollection
{
    /// <summary>
    /// Either of the policy sets listed here must be satisfied.
    /// </summary>
    public required PolicySet[] PolicySets { get; init; }
}
