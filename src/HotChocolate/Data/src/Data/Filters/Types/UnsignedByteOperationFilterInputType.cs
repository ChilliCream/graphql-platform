using HotChocolate.Data.Filters;
using HotChocolate.Types;

namespace HotChocolate.Data;

public class UnsignedByteOperationFilterInputType
    : ComparableOperationFilterInputType<UnsignedByteType>
{
    protected override void Configure(IFilterInputTypeDescriptor descriptor)
    {
        descriptor.Name("UnsignedByteOperationFilterInput");
        base.Configure(descriptor);
    }
}
