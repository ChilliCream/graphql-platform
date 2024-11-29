using System.Reflection;
using HotChocolate.Types.Descriptors;

#nullable enable

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
        ICustomAttributeProvider element)
    {
        if (descriptor is IScalarTypeDescriptor d
            && element is Type t)
        {
            OnConfigure(context, d, t);
        }
    }

    protected abstract void OnConfigure(
        IDescriptorContext context,
        IScalarTypeDescriptor descriptor,
        Type type);
}
