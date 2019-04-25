using System;
using System.Threading.Tasks;
using HotChocolate.Resolvers;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Relay
{
    internal sealed class NodeResolver<TNode, TId>
        : INodeResolver
    {
        private readonly NodeResolverDelegate<TNode, TId> _resolver;

        public NodeResolver(NodeResolverDelegate<TNode, TId> resolver)
        {
            _resolver = resolver
                ?? throw new ArgumentNullException(nameof(resolver));
        }

        public Task<TNode> ResolveAsync(IResolverContext context, TId id)
        {
            return _resolver.Invoke(context, id);
        }

        async Task<object> INodeResolver.ResolveAsync(
            IResolverContext context, object id)
        {
            if (id is TId c)
            {
                return await ResolveAsync(context, c).ConfigureAwait(false);
            }

            ITypeConversion typeConversion =
                context.Service<IServiceProvider>().GetTypeConversion();
            c = typeConversion.Convert<object, TId>(id);
            return await ResolveAsync(context, c).ConfigureAwait(false);
        }
    }
}
