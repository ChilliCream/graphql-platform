using System.Reflection;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types;

/// <summary>
/// Indicates that the given field, argument, input field, or enum value requires giving explicit
/// consent before being used.
/// </summary>
[AttributeUsage(
    AttributeTargets.Field // Required for enum values
    | AttributeTargets.Method
    | AttributeTargets.Parameter
    | AttributeTargets.Property,
    AllowMultiple = true)]
public sealed class RequiresOptInAttribute : DescriptorAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RequiresOptInAttribute"/>
    /// with a specific feature name and stability.
    /// </summary>
    /// <param name="feature">The name of the feature that requires opt in.</param>
    public RequiresOptInAttribute(string feature)
    {
        Feature = feature ?? throw new ArgumentNullException(nameof(feature));
    }

    /// <summary>
    /// The name of the feature that requires opt in.
    /// </summary>
    public string Feature { get; }

    protected internal override void TryConfigure(
        IDescriptorContext context,
        IDescriptor descriptor,
        ICustomAttributeProvider element)
    {
        switch (descriptor)
        {
            case IObjectFieldDescriptor desc:
                desc.RequiresOptIn(Feature);
                break;

            case IInputFieldDescriptor desc:
                desc.RequiresOptIn(Feature);
                break;

            case IArgumentDescriptor desc:
                desc.RequiresOptIn(Feature);
                break;

            case IEnumValueDescriptor desc:
                desc.RequiresOptIn(Feature);
                break;

            default:
                throw new SchemaException(
                    SchemaErrorBuilder.New()
                        .SetMessage(TypeResources.RequiresOptInDirective_Descriptor_NotSupported)
                        .SetExtension("member", element)
                        .SetExtension("descriptor", descriptor)
                        .Build());
        }
    }
}
