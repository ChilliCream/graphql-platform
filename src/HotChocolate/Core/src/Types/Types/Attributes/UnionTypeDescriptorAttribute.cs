using System.Reflection;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types;

[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Interface,
    Inherited = true,
    AllowMultiple = true)]
public abstract class UnionTypeDescriptorAttribute
    : DescriptorAttribute
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

        if (descriptor is not IUnionTypeDescriptor unionTypeDescriptor)
        {
            throw new InvalidOperationException("The descriptor must be of type IUnionTypeDescriptor.");
        }

        OnConfigure(context, unionTypeDescriptor, type);
    }

    protected abstract void OnConfigure(
        IDescriptorContext context,
        IUnionTypeDescriptor descriptor,
        Type? type);
}
