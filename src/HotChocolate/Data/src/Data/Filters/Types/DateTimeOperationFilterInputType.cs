using HotChocolate.Data.Filters;
using HotChocolate.Types;

namespace HotChocolate.Data;

public class DateTimeOperationFilterInputType
    : ComparableOperationFilterInputType<DateTimeType>
{
    protected override void Configure(IFilterInputTypeDescriptor descriptor)
    {
        descriptor.Name("DateTimeOperationFilterInput");
        base.Configure(descriptor);
    }
}
