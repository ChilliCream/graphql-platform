using HotChocolate.Data.Filters;
using HotChocolate.Types;

namespace HotChocolate.Data;

public class UrlOperationFilterInputType
    : ComparableOperationFilterInputType<UrlType>
{
    protected override void Configure(IFilterInputTypeDescriptor descriptor)
    {
        descriptor.Name("UrlOperationFilterInput");
        base.Configure(descriptor);
    }
}
