using HotChocolate.Data.MongoDb;
using HotChocolate.Data.Projections;

namespace HotChocolate.Data
{
    public static class MongoDbProjectionConventionDescriptorExtensions
    {
        /// <summary>
        /// Initializes the default configuration for MongoDb
        /// </summary>
        /// <param name="descriptor">The descriptor where the handlers are registered</param>
        /// <returns>The <paramref name="descriptor"/></returns>
        public static IProjectionConventionDescriptor AddMongoDbDefaults(
            this IProjectionConventionDescriptor descriptor) =>
            descriptor.Provider(new MongoDbProjectionProvider(x => x.AddMongoDbDefaults()));
    }
}
