using HotChocolate.Data.Projections;

namespace HotChocolate.Data.MongoDb
{
    public static class MongoDbProjectionConventionDescriptorExtensions
    {
        public static IProjectionConventionDescriptor AddMongoDbDefaults(
            this IProjectionConventionDescriptor descriptor) =>
            descriptor.Provider(new MongoDbProjectionProvider(x => x.AddMongoDbDefaults()));
    }
}
