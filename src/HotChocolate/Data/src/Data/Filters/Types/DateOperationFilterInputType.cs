using HotChocolate.Data.Filters;
using HotChocolate.Types;

namespace HotChocolate.Data;

public class DateOperationFilterInputType
    : ComparableOperationFilterInputType<DateType>
{
    protected override void Configure(IFilterInputTypeDescriptor descriptor)
    {
        descriptor.Name("DateOperationFilterInput");
        base.Configure(descriptor);
    }
}
