using System.Collections.Generic;
using System.Threading;

namespace HotChocolate.Resolvers.CodeGeneration
{
    internal interface IResolverBuilder
    {
        void AddDescriptors(IEnumerable<IDelegateDescriptor> descriptors);
        BuilderResult Build();
    }

    internal class BuilderResult
    {
        public IReadOnlyCollection<FieldResolver> Resolvers { get; }
        public IReadOnlyCollection<IDirectiveMiddleware> Middlewares { get; }
    }

    internal interface IFieldResolverBuilder
    {
        IReadOnlyCollection<FieldResolver> Build(
            IEnumerable<IFieldResolverDescriptor> descriptors);
    }

    internal interface IDirectiveMiddlewareBuilder
    {
        IReadOnlyCollection<IDirectiveMiddleware> Build(
            IEnumerable<IDirectiveMiddlewareDescriptor> descriptors);
    }
}
