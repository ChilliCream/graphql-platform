using System.Threading.Tasks;
using HotChocolate.Resolvers;

namespace HotChocolate.Types.Relay
{
    public delegate Task<TNode> NodeResolverDelegate<TNode>(
        IResolverContext context,
        object id);

    public delegate Task<TNode> NodeResolverDelegate<TNode, TId>(
        IResolverContext context,
        TId id);
}
