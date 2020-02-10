using System.Collections.Generic;

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    internal static class MetadataAnnotationFactory
    {
        public static IEnumerable<IResolverMetadataAnnotator> Create()
        {
            return CreateFor<IResolverContext>();
        }

        private static IEnumerable<IResolverMetadataAnnotator> CreateFor<T>()
            where T : IResolverContext
        {
            yield return new GetParentMetadataAnnotator<T>();
            yield return new GetContextMetadataAnnotator<T, IResolverContext>();
        }
    }
}
