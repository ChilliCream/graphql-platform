using System;
using System.Reflection;

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    internal interface IResolverMetadataAnnotator
    {
        bool CanHandle(
            ParameterInfo parameter,
            Type sourceType);

        ResolverMetadata Annotate(
            ResolverMetadata metadata,
            ParameterInfo parameter,
            Type sourceType);
    }
}
