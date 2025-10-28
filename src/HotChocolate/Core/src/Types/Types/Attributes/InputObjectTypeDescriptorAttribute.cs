using System.Reflection;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types;

[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Struct,
    Inherited = true,
    AllowMultiple = true)]
public abstract class InputObjectTypeDescriptorAttribute
    : DescriptorAttribute
{
    protected internal sealed override void TryConfigure(
        IDescriptorContext context,
        IDescriptor descriptor,
        ICustomAttributeProvider attributeProvider)
    {
        if (descriptor is IInputObjectTypeDescriptor d
            && attributeProvider is Type t)
        {
            OnConfigure(context, d, t);
        }
    }

    protected abstract void OnConfigure(
        IDescriptorContext context,
        IInputObjectTypeDescriptor descriptor,
        Type type);
}
