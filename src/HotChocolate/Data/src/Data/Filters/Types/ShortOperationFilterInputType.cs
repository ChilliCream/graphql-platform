using HotChocolate.Data.Filters;
using HotChocolate.Types;

namespace HotChocolate.Data;

public class ShortOperationFilterInputType
    : ComparableOperationFilterInputType<ShortType>
{
    protected override void Configure(IFilterInputTypeDescriptor descriptor)
    {
        descriptor.Name("ShortOperationFilterInput");
        base.Configure(descriptor);
    }
}
