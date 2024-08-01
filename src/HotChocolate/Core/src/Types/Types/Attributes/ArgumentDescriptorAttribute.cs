using System.Reflection;
using HotChocolate.Types.Descriptors;

#nullable enable

namespace HotChocolate.Types;

[AttributeUsage(
    AttributeTargets.Parameter,
    Inherited = true,
    AllowMultiple = true)]
public abstract class ArgumentDescriptorAttribute : DescriptorAttribute
{
    protected internal sealed override void TryConfigure(
        IDescriptorContext context,
        IDescriptor descriptor,
        ICustomAttributeProvider element)
    {
        if (descriptor is IArgumentDescriptor d
            && element is ParameterInfo p)
        {
            OnConfigure(context, d, p);
        }
    }

    protected abstract void OnConfigure(
        IDescriptorContext context,
        IArgumentDescriptor descriptor,
        ParameterInfo parameter);
}
