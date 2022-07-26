using HotChocolate.Data.Filters;

namespace HotChocolate.Data;

internal class QueryableFilterConventionDescriptor
    : FilterConventionDescriptorProxy
    , IQueryableFilterConventionDescriptor
{
    public QueryableFilterConventionDescriptor(IFilterConventionDescriptor descriptor) :
        base(descriptor)
    {
    }
}
