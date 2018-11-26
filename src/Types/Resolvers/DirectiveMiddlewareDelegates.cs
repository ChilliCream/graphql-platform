using System.Threading.Tasks;

namespace HotChocolate.Resolvers
{
    public delegate DirectiveDelegate DirectiveMiddleware(
        DirectiveDelegate next);

    public delegate Task DirectiveDelegate(
        IDirectiveContext context);
}
