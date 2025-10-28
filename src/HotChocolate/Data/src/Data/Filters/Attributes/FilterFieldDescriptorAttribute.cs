using System.Reflection;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Filters;

[AttributeUsage(
    AttributeTargets.Property | AttributeTargets.Method,
    Inherited = true,
    AllowMultiple = true)]
public abstract class FilterFieldDescriptorAttribute
    : DescriptorAttribute
{
    protected internal sealed override void TryConfigure(
        IDescriptorContext context,
        IDescriptor descriptor,
        ICustomAttributeProvider attributeProvider)
    {
        if (descriptor is IFilterFieldDescriptor d
            && attributeProvider is MemberInfo m)
        {
            OnConfigure(context, d, m);
        }
    }

    public abstract void OnConfigure(
        IDescriptorContext context,
        IFilterFieldDescriptor descriptor,
        MemberInfo member);
}
