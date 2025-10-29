using System.Reflection;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types;

[AttributeUsage(
    AttributeTargets.Field,
    Inherited = true,
    AllowMultiple = true)]
public abstract class EnumValueDescriptorAttribute : DescriptorAttribute
{
    protected internal sealed override void TryConfigure(
        IDescriptorContext context,
        IDescriptor descriptor,
        ICustomAttributeProvider? attributeProvider)
    {
        var field = attributeProvider as FieldInfo;

        if (RequiresAttributeProvider && field is null)
        {
            throw new InvalidOperationException("The attribute provider is required to be a field.");
        }

        if (descriptor is not IEnumValueDescriptor enumValueDescriptor)
        {
            throw new InvalidOperationException("The descriptor must be of type IEnumValueDescriptor.");
        }

        OnConfigure(context, enumValueDescriptor, field);
    }

    protected abstract void OnConfigure(
        IDescriptorContext context,
        IEnumValueDescriptor descriptor,
        FieldInfo? field);
}
