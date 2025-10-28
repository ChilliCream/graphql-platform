using HotChocolate.Types.Composite;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Pagination;

internal sealed class ConditionalShareableAttribute : ObjectTypeDescriptorAttribute
{
    protected override void OnConfigure(
        IDescriptorContext context,
        IObjectTypeDescriptor descriptor,
        Type type)
    {
        if (context.Options.ApplyShareableToPageInfo)
        {
            descriptor.Directive(Shareable.Instance);
        }
    }
}
