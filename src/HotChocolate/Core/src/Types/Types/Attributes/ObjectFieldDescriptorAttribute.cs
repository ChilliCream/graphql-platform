using System.Reflection;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types;

[AttributeUsage(
    AttributeTargets.Property | AttributeTargets.Method,
    Inherited = true,
    AllowMultiple = true)]
public abstract class ObjectFieldDescriptorAttribute : DescriptorAttribute
{
    protected internal sealed override void TryConfigure(
        IDescriptorContext context,
        IDescriptor descriptor,
        ICustomAttributeProvider attributeProvider)
    {
        if (descriptor is IObjectFieldDescriptor d
            && attributeProvider is MemberInfo m)
        {
            OnConfigure(context, d, m);
        }
    }

    protected abstract void OnConfigure(
        IDescriptorContext context,
        IObjectFieldDescriptor descriptor,
        MemberInfo member);
}
