using HotChocolate.Data.Filters;
using HotChocolate.Types;

namespace HotChocolate.Data;

public class UnsignedShortOperationFilterInputType
    : ComparableOperationFilterInputType<UnsignedShortType>
{
    protected override void Configure(IFilterInputTypeDescriptor descriptor)
    {
        descriptor.Name("UnsignedShortOperationFilterInput");
        base.Configure(descriptor);
    }
}
