using System.Reflection;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types;

[AttributeUsage(
    AttributeTargets.Class,
    Inherited = true,
    AllowMultiple = true)]
public abstract class ScalarTypeDescriptorAttribute : DescriptorAttribute
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

        if (descriptor is IScalarTypeDescriptor scalarTypeDescriptor)
        {
            OnConfigure(context, scalarTypeDescriptor, type);
        }
    }

    protected abstract void OnConfigure(
        IDescriptorContext context,
        IScalarTypeDescriptor descriptor,
        Type? type);
}
