using System.Threading.Tasks;
using HotChocolate.Resolvers;

namespace HotChocolate.Types.Payload
{
    internal class PayloadMiddleware
    {
        public static string MiddlewareIdentifier = "HotChocolate.Types.Payload.PayloadMiddleware";

        private readonly FieldDelegate _next;

        public PayloadMiddleware(FieldDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(IMiddlewareContext context)
        {
            await _next(context).ConfigureAwait(false);
            context.Result = new Payload(context.Result);
        }
    }
}
