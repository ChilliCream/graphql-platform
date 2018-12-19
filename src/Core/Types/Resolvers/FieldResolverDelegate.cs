using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Resolvers
{
    public delegate Task<object> FieldResolverDelegate(
        IResolverContext context);

    public delegate FieldDelegate FieldMiddleware(
        FieldDelegate next);

    // TODO : naming
    public delegate Task FieldDelegate(
        IMiddlewareContext context);
}
