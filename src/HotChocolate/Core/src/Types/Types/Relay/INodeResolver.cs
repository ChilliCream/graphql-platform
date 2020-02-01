using System.Threading.Tasks;
using HotChocolate.Resolvers;

namespace HotChocolate.Types.Relay
{
    public interface INodeResolver
    {
        Task<object> ResolveAsync(IResolverContext context, object id);
    }
}
