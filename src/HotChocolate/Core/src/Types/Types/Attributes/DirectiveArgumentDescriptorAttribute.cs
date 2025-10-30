using System.Reflection;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public abstract class DirectiveArgumentDescriptorAttribute : DescriptorAttribute
{
    protected internal sealed override void TryConfigure(
        IDescriptorContext context,
        IDescriptor descriptor,
        ICustomAttributeProvider? attributeProvider)
    {
        var property = attributeProvider as PropertyInfo;

        if (RequiresAttributeProvider && property is null)
        {
            throw new InvalidOperationException("The attribute provider is required to be a property.");
        }

        if (descriptor is not IDirectiveArgumentDescriptor directiveArgumentDescriptor)
        {
            throw new InvalidOperationException("The descriptor must be of type IDirectiveArgumentDescriptor.");
        }

        OnConfigure(context, directiveArgumentDescriptor, property);
    }

    protected abstract void OnConfigure(
        IDescriptorContext context,
        IDirectiveArgumentDescriptor descriptor,
        PropertyInfo? property);
}
