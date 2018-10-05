using System.Threading.Tasks;

namespace HotChocolate.Resolvers
{
    public delegate DirectiveDelegate Middleware(
        DirectiveDelegate next);

    public delegate Task DirectiveDelegate(
        IDirectiveContext context);
}
