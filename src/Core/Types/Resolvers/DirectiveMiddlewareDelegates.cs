using System.Threading.Tasks;

namespace HotChocolate.Resolvers
{
    public delegate DirectiveDelegate DirectiveMiddleware(
        FieldDelegate next);

    // TODO : InvokeDirectiveDelegate
    public delegate Task DirectiveDelegate(
        IDirectiveContext context);
}
