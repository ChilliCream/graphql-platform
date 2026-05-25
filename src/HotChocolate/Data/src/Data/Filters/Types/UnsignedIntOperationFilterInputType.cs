using HotChocolate.Data.Filters;
using HotChocolate.Types;

namespace HotChocolate.Data;

public class UnsignedIntOperationFilterInputType
    : ComparableOperationFilterInputType<UnsignedIntType>
{
    protected override void Configure(IFilterInputTypeDescriptor descriptor)
    {
        descriptor.Name("UnsignedIntOperationFilterInput");
        base.Configure(descriptor);
    }
}
