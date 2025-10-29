using System.Reflection;
using HotChocolate.Types.Descriptors;

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
        ICustomAttributeProvider? attributeProvider)
    {
        var type = attributeProvider as Type;

        if (RequiresAttributeProvider && type is null)
        {
            throw new InvalidOperationException("The attribute provider is required to be a type.");
        }

        if (descriptor is not IObjectTypeDescriptor objectTypeDescriptor)
        {
            throw new InvalidOperationException("The descriptor must be of type IObjectTypeDescriptor.");
        }

        OnConfigure(context, objectTypeDescriptor, type);
    }

    protected abstract void OnConfigure(
        IDescriptorContext context,
        IObjectTypeDescriptor descriptor,
        Type? type);
}
