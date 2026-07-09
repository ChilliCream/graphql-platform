using HotChocolate.Data.Filters;
using HotChocolate.Types;

namespace HotChocolate.Data;

public class UriOperationFilterInputType
    : ComparableOperationFilterInputType<UriType>
{
    protected override void Configure(IFilterInputTypeDescriptor descriptor)
    {
        descriptor.Name("UriOperationFilterInput");
        base.Configure(descriptor);
    }
}
