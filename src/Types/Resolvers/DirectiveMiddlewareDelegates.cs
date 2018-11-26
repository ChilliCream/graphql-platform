using System.Threading.Tasks;

namespace HotChocolate.Resolvers
{
    public delegate DirectiveDelegate DirectiveMiddleware(
        DirectiveDelegate next);

    public delegate Task DirectiveDelegate(
        IDirectiveContext context);



    public static class Dummy
    {
        public static void Foo()
        {
            FieldMiddleware m = next => async (context, ct) =>
            {
                object result = await next.Invoke(context, ct);
                return result;
            }
        }
    }
}


