using HotChocolate.Data.Filters;
using HotChocolate.Types;

namespace HotChocolate.Data;

public class FloatOperationFilterInputType
    : ComparableOperationFilterInputType<FloatType>
{
    protected override void Configure(IFilterInputTypeDescriptor descriptor)
    {
        descriptor.Name("FloatOperationFilterInput");
        base.Configure(descriptor);
    }
}
