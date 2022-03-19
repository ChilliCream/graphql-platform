using HotChocolate.Data.Filters;
using HotChocolate.Types;

namespace HotChocolate.Data;

public class IntOperationFilterInputType
    : ComparableOperationFilterInputType<IntType>
{
    protected override void Configure(IFilterInputTypeDescriptor descriptor)
    {
        descriptor.Name("IntOperationFilterInput");
        base.Configure(descriptor);
    }
}
