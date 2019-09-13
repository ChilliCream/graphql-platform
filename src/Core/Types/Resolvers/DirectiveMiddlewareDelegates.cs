using System.Threading.Tasks;

namespace HotChocolate.Resolvers
{
    public delegate DirectiveDelegate DirectiveMiddleware(
        FieldDelegate next);

    public delegate Task DirectiveDelegate(
        IDirectiveContext context);
}
