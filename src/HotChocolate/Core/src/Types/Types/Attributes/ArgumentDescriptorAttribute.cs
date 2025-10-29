using System.Reflection;
using HotChocolate.Types.Descriptors;

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
        ICustomAttributeProvider? attributeProvider)
    {
        var parameter = attributeProvider as ParameterInfo;

        if (RequiresAttributeProvider && parameter is null)
        {
            throw new InvalidOperationException("The attribute provider is required to be a parameter.");
        }

        if (descriptor is not IArgumentDescriptor argumentDescriptor)
        {
            throw new InvalidOperationException("The descriptor must be of type IArgumentDescriptor.");
        }

        OnConfigure(context, argumentDescriptor, parameter);
    }

    protected abstract void OnConfigure(
        IDescriptorContext context,
        IArgumentDescriptor descriptor,
        ParameterInfo? parameter);
}
