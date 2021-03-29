using System.Threading.Tasks;
using HotChocolate.Resolvers;

namespace HotChocolate.Types.Relay
{
    /// <summary>
    /// The node resolver is used to resolve a specific node type by the specified id.
    /// </summary>
    public interface INodeResolver
    {
        /// <summary>
        /// Resolves a new instance by the specified <paramref name="id"/>.
        /// </summary>
        /// <param name="context">
        /// The resolver context.
        /// </param>
        /// <param name="id">
        /// The id runtime value.
        /// </param>
        /// <returns>
        /// Returns the resolver node.
        /// </returns>
        Task<object> ResolveAsync(IResolverContext context, object id);
    }
}
