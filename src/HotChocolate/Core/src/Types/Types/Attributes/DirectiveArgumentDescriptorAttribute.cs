#nullable enable
using System.Reflection;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public abstract class DirectiveArgumentDescriptorAttribute : DescriptorAttribute
{
    protected internal sealed override void TryConfigure(
        IDescriptorContext context,
        IDescriptor descriptor,
        ICustomAttributeProvider element)
    {
        if (descriptor is IDirectiveArgumentDescriptor d
            && element is PropertyInfo property)
        {
            OnConfigure(context, d, property);
        }
    }

    protected abstract void OnConfigure(
        IDescriptorContext context,
        IDirectiveArgumentDescriptor descriptor,
        PropertyInfo property);
}
