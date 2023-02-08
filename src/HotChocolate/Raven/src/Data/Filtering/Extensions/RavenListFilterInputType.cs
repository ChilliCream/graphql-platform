using HotChocolate.Data.Filters;

namespace HotChocolate.Data;

internal class RavenListFilterInputType<T>
    : ListFilterInputType<T>
    where T : FilterInputType
{
    protected override void Configure(IFilterInputTypeDescriptor descriptor)
    {
        descriptor.Operation(DefaultFilterOperations.All).Ignore();
        base.Configure(descriptor);
    }
}
