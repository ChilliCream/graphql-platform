using System.Threading.Tasks;

namespace HotChocolate.Resolvers
{
    public delegate DirectiveDelegate DirectiveMiddleware(
        DirectiveDelegate next);

    // TODO : InvokeDirectiveDelegate
    public delegate Task DirectiveDelegate(
        IDirectiveContext context);
}
