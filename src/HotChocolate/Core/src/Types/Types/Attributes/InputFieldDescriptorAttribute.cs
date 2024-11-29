using System.Reflection;
using HotChocolate.Types.Descriptors;

#nullable enable

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
        ICustomAttributeProvider element)
    {
        if (descriptor is IInputFieldDescriptor d
            && element is MemberInfo m)
        {
            OnConfigure(context, d, m);
        }
    }

    protected abstract void OnConfigure(
        IDescriptorContext context,
        IInputFieldDescriptor descriptor,
        MemberInfo member);
}
