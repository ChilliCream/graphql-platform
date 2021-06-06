using HotChocolate.Data.Projections;

namespace HotChocolate.Data.Neo4J.Projections
{
    public static class Neo4JProjectionConventionDescriptorExtensions
    {
        /// <summary>
        /// Initializes the default configuration for Neo4J
        /// </summary>
        /// <param name="descriptor">The descriptor where the handlers are registered</param>
        /// <returns>The <paramref name="descriptor"/></returns>
        public static IProjectionConventionDescriptor AddNeo4JDefaults(
            this IProjectionConventionDescriptor descriptor) =>
            descriptor.Provider(new Neo4JProjectionProvider(x => x.AddNeo4JDefaults()));
    }
}
