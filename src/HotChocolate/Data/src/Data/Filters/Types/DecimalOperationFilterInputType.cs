using HotChocolate.Data.Filters;
using HotChocolate.Types;

namespace HotChocolate.Data;

public class DecimalOperationFilterInputType
    : ComparableOperationFilterInputType<DecimalType>
{
    protected override void Configure(IFilterInputTypeDescriptor descriptor)
    {
        descriptor.Name("DecimalOperationFilterInput");
        base.Configure(descriptor);
    }
}
