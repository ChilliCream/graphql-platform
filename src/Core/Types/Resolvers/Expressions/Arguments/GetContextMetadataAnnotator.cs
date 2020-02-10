using System;
using System.Reflection;

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    internal class GetContextMetadataAnnotator<T, TContext>
          : ResolverMetadataAnnotatorBase<T>
          where T : IResolverContext
    {
        public override bool CanHandle(
            ParameterInfo parameter,
            Type sourceType) =>
            parameter.ParameterType.IsAssignableFrom(typeof(TContext));

        public override ResolverMetadata Annotate(
            ResolverMetadata metadata,
            ParameterInfo parameter,
            Type sourceType)
        {
            return metadata.AsNotPure();
        }
    }
}
