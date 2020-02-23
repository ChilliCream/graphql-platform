using System;
using System.Reflection;
using HotChocolate.Resolvers.CodeGeneration;

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    internal class GetParentMetadataAnnotator<T>
        : ResolverMetadataAnnotatorBase<T>
        where T : IResolverContext
    {
        public override bool CanHandle(
            ParameterInfo parameter,
            Type sourceType) =>
            ArgumentHelper.IsParent(parameter, sourceType);

        public override ResolverMetadata Annotate(
            ResolverMetadata metadata,
            ParameterInfo parameter,
            Type sourceType)
        {
            return metadata.AsNonPure();
        }
    }
}
