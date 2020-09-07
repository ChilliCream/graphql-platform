using System;
using System.Threading.Tasks;
using HotChocolate.Resolvers;

namespace HotChocolate.Types.Relay
{
    public class IdMiddleware
    {
        private readonly IIdSerializer _serializer;
        private readonly FieldDelegate _next;

        public IdMiddleware(FieldDelegate next, IIdSerializer serializer)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _serializer = serializer ?? new IdSerializer();
        }

        public async Task InvokeAsync(IMiddlewareContext context)
        {
            await _next(context).ConfigureAwait(false);

            context.Result = _serializer.Serialize(
                context.Schema.Name,
                context.ObjectType.Name,
                context.Result);
        }
    }
}
