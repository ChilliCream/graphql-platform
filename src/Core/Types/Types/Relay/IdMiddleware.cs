using System;
using System.Threading.Tasks;
using HotChocolate.Resolvers;

namespace HotChocolate.Types.Relay
{
    public class IdMiddleware
    {
        private static readonly string _idFieldName = new NameString("id");
        private readonly IdSerializer _serializer = new IdSerializer();
        private readonly FieldDelegate _next;

        public IdMiddleware(FieldDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public async Task InvokeAsync(IMiddlewareContext context)
        {
            await _next(context).ConfigureAwait(false);

            if (context.Result != null
                && context.Field.Type.NamedType() is IdType
                && context.Field.Name.Equals(_idFieldName))
            {
                context.Result = _serializer.Serialize(
                    context.ObjectType.Name,
                    context.Result);
            }
        }
    }
}
