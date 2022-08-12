using HotChocolate.Data.Filters;
using HotChocolate.Types;

namespace HotChocolate.Data;

public class UuidOperationFilterInputType
    : ComparableOperationFilterInputType<UuidType>
{
    protected override void Configure(IFilterInputTypeDescriptor descriptor)
    {
        descriptor.Name("UuidOperationFilterInput");
        base.Configure(descriptor);
    }
}
