using HotChocolate.Data.Filters;
using HotChocolate.Types;

namespace HotChocolate.Data;

public class ByteOperationFilterInputType
    : ComparableOperationFilterInputType<ByteType>
{
    protected override void Configure(IFilterInputTypeDescriptor descriptor)
    {
        descriptor.Name("ByteOperationFilterInput");
        base.Configure(descriptor);
    }
}
