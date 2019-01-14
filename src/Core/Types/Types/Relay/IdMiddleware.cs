using System;
using System.Threading.Tasks;
using HotChocolate.Resolvers;

namespace HotChocolate.Types.Relay
{
    public class IdMiddleware
    {
        private readonly IdSerializer _serializer = new IdSerializer();
        private readonly FieldDelegate _next;

        public IdMiddleware(FieldDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public async Task InvokeAsync(IMiddlewareContext context)
        {
            await _next(context);

            if (context.Result != null
                && context.Field.Type.NamedType() is IdType)
            {
                context.Result = _serializer.Serialize(
                    context.ObjectType.Name,
                    context.Result);
            }
        }


    }
}
