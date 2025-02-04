using HotChocolate.Data.Filters;
using HotChocolate.Types;

namespace HotChocolate.Data;

public class LocalDateOperationFilterInputType
    : ComparableOperationFilterInputType<LocalDateType>
{
    protected override void Configure(IFilterInputTypeDescriptor descriptor)
    {
        descriptor.Name("LocalDateOperationFilterInput");
        base.Configure(descriptor);
    }
}
