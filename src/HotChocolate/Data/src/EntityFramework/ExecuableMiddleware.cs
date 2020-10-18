using System.Threading.Tasks;
using HotChocolate.Resolvers;

namespace HotChocolate.Data
{
    internal class ExecuableMiddleware
    {
        private readonly FieldDelegate _next;

        public ExecuableMiddleware(FieldDelegate next)
        {
            _next = next;
        }

        public async ValueTask InvokeAsync(IMiddlewareContext context)
        {
            await _next(context).ConfigureAwait(false);

            if (context.Result is IExecutable executable)
            {
                context.Result = await executable
                    .ExecuteAsync(context.RequestAborted)
                    .ConfigureAwait(false);
            }
        }
    }
}
