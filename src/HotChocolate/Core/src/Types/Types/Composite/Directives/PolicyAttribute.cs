using System.Reflection;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Composite;

/// <summary>
/// Applies one @policy group to the annotated member. Repeat the attribute to add further
/// alternative groups.
/// </summary>
[AttributeUsage(
    AttributeTargets.Class
    | AttributeTargets.Struct
    | AttributeTargets.Interface
    | AttributeTargets.Enum
    | AttributeTargets.Method
    | AttributeTargets.Property,
    AllowMultiple = true)]
public sealed class PolicyAttribute : DescriptorAttribute
{
    /// <summary>
    /// Initializes a new instance of <see cref="PolicyAttribute"/>.
    /// </summary>
    /// <param name="policies">
    /// The policies that are required together.
    /// </param>
    public PolicyAttribute(params string[] policies)
    {
        Policies = policies ?? throw new ArgumentNullException(nameof(policies));
    }

    /// <summary>
    /// Gets the policies of this group.
    /// </summary>
    public IReadOnlyList<string> Policies { get; }

    protected internal override void TryConfigure(
        IDescriptorContext context,
        IDescriptor descriptor,
        ICustomAttributeProvider? attributeProvider)
    {
        var values = new string[Policies.Count];

        for (var i = 0; i < values.Length; i++)
        {
            values[i] = Policies[i];
        }

        switch (descriptor)
        {
            case IObjectTypeDescriptor desc:
                desc.Policy(values);
                break;

            case IObjectFieldDescriptor desc:
                desc.Policy(values);
                break;

            case IInterfaceTypeDescriptor desc:
                desc.Policy(values);
                break;

            case IInterfaceFieldDescriptor desc:
                desc.Policy(values);
                break;

            case IEnumTypeDescriptor desc:
                desc.Policy(values);
                break;

            case IScalarTypeDescriptor desc:
                desc.Policy(values);
                break;

            default:
                throw ThrowHelper.AuthorizationDirective_UnsupportedDescriptor(
                    DirectiveNames.Policy.Name,
                    attributeProvider,
                    descriptor);
        }
    }
}
