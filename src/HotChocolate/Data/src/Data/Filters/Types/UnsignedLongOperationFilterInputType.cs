using HotChocolate.Data.Filters;
using HotChocolate.Types;

namespace HotChocolate.Data;

public class UnsignedLongOperationFilterInputType
    : ComparableOperationFilterInputType<UnsignedLongType>
{
    protected override void Configure(IFilterInputTypeDescriptor descriptor)
    {
        descriptor.Name("UnsignedLongOperationFilterInput");
        base.Configure(descriptor);
    }
}
