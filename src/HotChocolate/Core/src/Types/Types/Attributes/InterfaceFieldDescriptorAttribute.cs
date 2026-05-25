using System.Reflection;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types;

[AttributeUsage(
    AttributeTargets.Property | AttributeTargets.Method,
    Inherited = true,
    AllowMultiple = true)]
public abstract class InterfaceFieldDescriptorAttribute
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

        if (descriptor is IInterfaceFieldDescriptor interfaceFieldDescriptor)
        {
            OnConfigure(context, interfaceFieldDescriptor, member);
        }
    }

    protected abstract void OnConfigure(
        IDescriptorContext context,
        IInterfaceFieldDescriptor descriptor,
        MemberInfo? member);
}
