using HotChocolate.Data.Filters;
using HotChocolate.Types;

namespace HotChocolate.Data;

public class DurationOperationFilterInputType
    : ComparableOperationFilterInputType<DurationType>
{
    protected override void Configure(IFilterInputTypeDescriptor descriptor)
    {
        descriptor.Name("DurationOperationFilterInput");
        base.Configure(descriptor);
    }
}
