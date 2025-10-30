using System.Reflection;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Sorting;

[AttributeUsage(
    AttributeTargets.Property | AttributeTargets.Method,
    Inherited = true,
    AllowMultiple = true)]
public abstract class SortFieldDescriptorAttribute
    : DescriptorAttribute
{
    protected internal sealed override void TryConfigure(
        IDescriptorContext context,
        IDescriptor descriptor,
        ICustomAttributeProvider? attributeProvider)
    {
        var member = attributeProvider as MemberInfo;

        if (RequiresAttributeProvider && member is null)
        {
            throw new InvalidOperationException("The attribute provider is required to be a member.");
        }

        if (descriptor is not ISortFieldDescriptor sortFieldDescriptor)
        {
            throw new InvalidOperationException("The descriptor must be of type ISortFieldDescriptor.");
        }

        OnConfigure(context, sortFieldDescriptor, member);
    }

    public abstract void OnConfigure(
        IDescriptorContext context,
        ISortFieldDescriptor descriptor,
        MemberInfo? member);
}
