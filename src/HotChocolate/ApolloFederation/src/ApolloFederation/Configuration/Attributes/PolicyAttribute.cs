using System.Reflection;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.ApolloFederation;

[AttributeUsage(
    AttributeTargets.Class
    | AttributeTargets.Interface
    | AttributeTargets.Property
    | AttributeTargets.Method
    | AttributeTargets.Enum
    | AttributeTargets.Field
    | AttributeTargets.Struct,
    Inherited = true,
    // TODO: Should we just allow multiple attributes instead maybe?
    AllowMultiple = false)]
public sealed class PolicyAttribute : DescriptorAttribute
{
    public PolicyAttribute(PolicyCollection policyCollection)
    {
        PolicyCollection = policyCollection;
    }

    public PolicyAttribute(params string[] policySets)
    {
        PolicyCollection = ConvertCommaSeparatedPolicyNamesListsToCollection(policySets);
    }

    /// <summary>
    /// </summary>
    public PolicyCollection PolicyCollection { get; set; }

    /// <summary>
    /// Comma-separated lists of policy names for each set.
    /// </summary>
    public string[] PolicySets
    {
        set => PolicyCollection = ConvertCommaSeparatedPolicyNamesListsToCollection(value);
    }

    private static PolicyCollection ConvertCommaSeparatedPolicyNamesListsToCollection(string[] names)
    {
        var policySets = new PolicySet[names.Length];
        var policySetCount = policySets.Length;
        for (var policySetIndex = 0; policySetIndex < policySetCount; policySetIndex++)
        {
            var commaSeparatedPolicyNames = names[policySetIndex];
            var policyNames = commaSeparatedPolicyNames.Split(',');
            var policyCount = policyNames.Length;
            var policies = new Policy[policyCount];
            for (var policyIndex = 0; policyIndex < policyCount; policyIndex++)
            {
                var name = policyNames[policyIndex].Trim();
                policies[policyIndex] = new Policy
                {
                    Name = name,
                };
            }
        }

        return new()
        {
            PolicySets = policySets,
        };
    }

    protected internal override void TryConfigure(
        IDescriptorContext context,
        IDescriptor descriptor,
        ICustomAttributeProvider element)
    {
        var policyCollection = PolicyCollection;

        switch (descriptor)
        {
            case IObjectFieldDescriptor fieldDescriptor:
            {
                fieldDescriptor.Policy(policyCollection);
                break;
            }
            case IEnumTypeDescriptor enumTypeDescriptor:
            {
                enumTypeDescriptor.Policy(policyCollection);
                break;
            }
            case IInterfaceTypeDescriptor interfaceTypeDescriptor:
            {
                interfaceTypeDescriptor.Policy(policyCollection);
                break;
            }
            case IObjectTypeDescriptor objectTypeDescriptor:
            {
                objectTypeDescriptor.Policy(policyCollection);
                break;
            }
            case IInterfaceFieldDescriptor interfaceFieldDescriptor:
            {
                interfaceFieldDescriptor.Policy(policyCollection);
                break;
            }
        }
    }
}
