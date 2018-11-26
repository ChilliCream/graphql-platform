using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Resolvers
{
    // public delegate Task<object> AsyncFieldResolverDelegate(
    //    IResolverContext context,
    //    CancellationToken cancellationToken);

    // public delegate object FieldResolverDelegate(
    //    IResolverContext context,
    //    CancellationToken cancellationToken);

    // TODO : naming
    public delegate Task<object> FieldDelegate(
        IResolverContext context);

    public delegate FieldDelegate FieldMiddleware(
        FieldDelegate next);
}
