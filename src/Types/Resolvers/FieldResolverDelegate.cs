using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Resolvers
{
    public delegate Task<object> FieldResolverDelegate(
        IResolverContext context);

    public delegate FieldResolverDelegate FieldMiddleware(
        FieldResolverDelegate next);
}
