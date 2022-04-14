using HotChocolate.Data.Filters;
using HotChocolate.Types;

namespace HotChocolate.Data;

public class LongOperationFilterInputType
    : ComparableOperationFilterInputType<LongType>
{
    protected override void Configure(IFilterInputTypeDescriptor descriptor)
    {
        descriptor.Name("LongOperationFilterInput");
        base.Configure(descriptor);
    }
}
