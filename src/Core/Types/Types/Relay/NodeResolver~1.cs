using System;
using System.Threading.Tasks;
using HotChocolate.Resolvers;

namespace HotChocolate.Types.Relay
{
    internal sealed class NodeResolver<TNode>
        : INodeResolver
    {
        private readonly NodeResolverDelegate<TNode> _resolver;

        public NodeResolver(NodeResolverDelegate<TNode> resolver)
        {
            _resolver = resolver
                ?? throw new ArgumentNullException(nameof(resolver));
        }

        public Task<TNode> ResolveAsync(IResolverContext context, object id)
        {
            return _resolver.Invoke(context, id);
        }

        async Task<object> INodeResolver.ResolveAsync(
            IResolverContext context, object id) =>
            await ResolveAsync(context, id).ConfigureAwait(false);
    }
}
