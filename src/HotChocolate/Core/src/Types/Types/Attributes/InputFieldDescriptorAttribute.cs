using System.Reflection;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types;

[AttributeUsage(
    AttributeTargets.Property | AttributeTargets.Method,
    Inherited = true,
    AllowMultiple = true)]
public abstract class InputFieldDescriptorAttribute
    : DescriptorAttribute
{
    protected internal sealed override void TryConfigure(
        IDescriptorContext context,
        IDescriptor descriptor,
        ICustomAttributeProvider? attributeProvider)
    {
        var member = attributeProvider as MemberInfo;

        if (RequiresAttributeProvider && member is null)
        {
            throw new InvalidOperationException("The attribute provider is required to be a member.");
        }

        if (descriptor is not IInputFieldDescriptor inputFieldDescriptor)
        {
            throw new InvalidOperationException("The descriptor must be of type IInputFieldDescriptor.");
        }

        OnConfigure(context, inputFieldDescriptor, member);
    }

    protected abstract void OnConfigure(
        IDescriptorContext context,
        IInputFieldDescriptor descriptor,
        MemberInfo? member);
}
