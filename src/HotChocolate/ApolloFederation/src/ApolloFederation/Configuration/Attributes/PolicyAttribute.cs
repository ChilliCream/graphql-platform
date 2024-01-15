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
    public PolicyAttribute(string[][] policyCollection)
    {
        PolicyCollection = policyCollection;
    }

    public PolicyAttribute(params string[] policySets)
    {
        PolicyCollection = ConvertCommaSeparatedPolicyNamesListsToCollection(policySets);
    }

    /// <summary>
    /// </summary>
    public string[][] PolicyCollection { get; set; }

    /// <summary>
    /// Comma-separated lists of policy names for each set.
    /// </summary>
    public string[] PolicySets
    {
        set => PolicyCollection = ConvertCommaSeparatedPolicyNamesListsToCollection(value);
    }

    private static string[][] ConvertCommaSeparatedPolicyNamesListsToCollection(string[] names)
    {
        var policySets = new string[names.Length][];
        var policySetCount = policySets.Length;
        for (var policySetIndex = 0; policySetIndex < policySetCount; policySetIndex++)
        {
            var commaSeparatedPolicyNames = names[policySetIndex];
            var policyNames = commaSeparatedPolicyNames.Split(',');
            policySets[policySetIndex] = policyNames;
        }
        return policySets;
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
