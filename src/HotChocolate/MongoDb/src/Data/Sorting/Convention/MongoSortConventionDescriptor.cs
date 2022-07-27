using HotChocolate.Data.Sorting;

namespace HotChocolate.Data.MongoDb.Sorting;

internal class MongoSortConventionDescriptor
    : SortConventionDescriptorProxy
    , IMongoSortConventionDescriptor
{
    public MongoSortConventionDescriptor(ISortConventionDescriptor descriptor) :
        base(descriptor)
    {
    }
}
