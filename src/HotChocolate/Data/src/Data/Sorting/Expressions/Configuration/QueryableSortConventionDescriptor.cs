namespace HotChocolate.Data.Sorting.Expressions;

internal class QueryableSortConventionDescriptor
    : SortConventionDescriptorProxy
    , IQueryableSortConventionDescriptor
{
    public QueryableSortConventionDescriptor(ISortConventionDescriptor descriptor) :
        base(descriptor)
    {
    }
}
