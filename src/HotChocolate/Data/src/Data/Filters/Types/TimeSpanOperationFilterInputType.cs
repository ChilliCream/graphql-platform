using HotChocolate.Data.Filters;
using HotChocolate.Types;

namespace HotChocolate.Data;

public class TimeSpanOperationFilterInputType
    : ComparableOperationFilterInputType<TimeSpanType>
{
    protected override void Configure(IFilterInputTypeDescriptor descriptor)
    {
        descriptor.Name("TimeSpanOperationFilterInput");
        base.Configure(descriptor);
    }
}
