using HotChocolate.Data.Filters;
using HotChocolate.Types;

namespace HotChocolate.Data;

public class LocalTimeOperationFilterInputType
    : ComparableOperationFilterInputType<LocalTimeType>
{
    protected override void Configure(IFilterInputTypeDescriptor descriptor)
    {
        descriptor.Name("LocalTimeOperationFilterInput");
        base.Configure(descriptor);
    }
}
