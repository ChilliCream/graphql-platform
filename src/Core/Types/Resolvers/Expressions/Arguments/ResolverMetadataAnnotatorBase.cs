using System;
using System.Reflection;

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    internal abstract class ResolverMetadataAnnotatorBase<T>
        : IResolverMetadataAnnotator
        where T : IResolverContext
    {
        protected ResolverMetadataAnnotatorBase()
        {
            ContextType = typeof(T);
            ContextTypeInfo = ContextType.GetTypeInfo();
        }

        protected Type ContextType { get; }

        protected TypeInfo ContextTypeInfo { get; }

        public abstract bool CanHandle(
            ParameterInfo parameter,
            Type sourceType);

        public abstract ResolverMetadata Annotate(
            ResolverMetadata metadata,
            ParameterInfo parameter,
            Type sourceType);
    }
}
