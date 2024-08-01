using System.Reflection;
using HotChocolate.Types.Descriptors;

#nullable enable

namespace HotChocolate.Types;

[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Struct,
    Inherited = true,
    AllowMultiple = true)]
public abstract class ObjectTypeDescriptorAttribute : DescriptorAttribute
{
    protected internal sealed override void TryConfigure(
        IDescriptorContext context,
        IDescriptor descriptor,
        ICustomAttributeProvider element)
    {
        if (descriptor is IObjectTypeDescriptor d
            && element is Type t)
        {
            OnConfigure(context, d, t);
        }
    }

    protected abstract void OnConfigure(
        IDescriptorContext context,
        IObjectTypeDescriptor descriptor,
        Type type);
}
