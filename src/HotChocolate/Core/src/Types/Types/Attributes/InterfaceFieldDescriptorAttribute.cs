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
        ICustomAttributeProvider attributeProvider)
    {
        if (descriptor is IInterfaceFieldDescriptor d
            && attributeProvider is MemberInfo m)
        {
            OnConfigure(context, d, m);
        }
    }

    protected abstract void OnConfigure(
        IDescriptorContext context,
        IInterfaceFieldDescriptor descriptor,
        MemberInfo member);
}
