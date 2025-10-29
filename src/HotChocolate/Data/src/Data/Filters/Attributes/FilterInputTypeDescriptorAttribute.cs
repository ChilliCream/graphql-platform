using System.Reflection;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Filters;

[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Struct,
    Inherited = true,
    AllowMultiple = true)]
public abstract class FilterInputTypeDescriptorAttribute
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

        if (descriptor is not IFilterInputTypeDescriptor filterInputTypeDescriptor)
        {
            throw new InvalidOperationException("The descriptor must be of type IFilterInputTypeDescriptor.");
        }

        OnConfigure(context, filterInputTypeDescriptor, type);
    }

    public abstract void OnConfigure(
        IDescriptorContext context,
        IFilterInputTypeDescriptor descriptor,
        Type? type);
}
