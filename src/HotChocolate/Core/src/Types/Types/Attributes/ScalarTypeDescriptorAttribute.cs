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
        ICustomAttributeProvider attributeProvider)
    {
        if (descriptor is IScalarTypeDescriptor d
            && attributeProvider is Type t)
        {
            OnConfigure(context, d, t);
        }
    }

    protected abstract void OnConfigure(
        IDescriptorContext context,
        IScalarTypeDescriptor descriptor,
        Type type);
}
