using System.Threading.Tasks;

namespace HotChocolate.Execution
{
    public delegate RequestDelegate RequestMiddleware(RequestDelegate next);

    public delegate Task RequestDelegate(IRequestContext context);
}
