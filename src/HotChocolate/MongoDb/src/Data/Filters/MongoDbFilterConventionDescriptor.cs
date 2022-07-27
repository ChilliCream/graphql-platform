using HotChocolate.Data.Filters;

namespace HotChocolate.Data.MongoDb.Filters;

internal class MongoDbFilterConventionDescriptor
    : FilterConventionDescriptorProxy
    , IMongoDbFilterConventionDescriptor
{
    public MongoDbFilterConventionDescriptor(IFilterConventionDescriptor descriptor) :
        base(descriptor)
    {
    }
}
