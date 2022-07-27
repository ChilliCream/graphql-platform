using HotChocolate.Data.Projections;

namespace HotChocolate.Data.MongoDb.Projections;

internal class MongoProjectionConventionDescriptor
    : ProjectionConventionDescriptorProxy
    , IMongoProjectionConventionDescriptor
{
    public MongoProjectionConventionDescriptor(IProjectionConventionDescriptor descriptor) :
        base(descriptor)
    {
    }
}
