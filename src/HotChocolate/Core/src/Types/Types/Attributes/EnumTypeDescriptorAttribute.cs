using System.Reflection;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types;

[AttributeUsage(
    AttributeTargets.Enum | AttributeTargets.Class | AttributeTargets.Struct,
    Inherited = true,
    AllowMultiple = true)]
public abstract class EnumTypeDescriptorAttribute : DescriptorAttribute
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

        if (descriptor is not IEnumTypeDescriptor enumTypeDescriptor)
        {
            throw new InvalidOperationException("The descriptor must be of type IEnumTypeDescriptor.");
        }

        OnConfigure(context, enumTypeDescriptor, type);
    }

    protected abstract void OnConfigure(
        IDescriptorContext context,
        IEnumTypeDescriptor descriptor,
        Type? type);
}
