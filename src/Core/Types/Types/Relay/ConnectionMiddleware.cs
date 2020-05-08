using System;
using System.Threading.Tasks;
using HotChocolate.Resolvers;

namespace HotChocolate.Types.Relay
{
    public class ConnectionMiddleware<T>
    {
        private readonly FieldDelegate _next;

        public ConnectionMiddleware(FieldDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public async Task InvokeAsync(
            IMiddlewareContext context,
            IConnectionResolver<T> connectionResolver)
        {
            await _next(context).ConfigureAwait(false);

            if (context.Result is IConnectionResolver localConnectionResolver)
            {
                context.Result = localConnectionResolver.ResolveAsync(
                    context,
                    default,
                    context.Argument<int?>("first"),
                    context.Argument<int?>("last"),
                    context.Argument<string>("after"),
                    context.Argument<string>("before"),
                    context.RequestAborted)
                    .ConfigureAwait(false);
            }

            if (connectionResolver is { } && context.Result is T item)
            {
                context.Result = await connectionResolver.ResolveAsync(
                    context,
                    default,
                    context.Argument<int?>("first"),
                    context.Argument<int?>("last"),
                    context.Argument<string>("after"),
                    context.Argument<string>("before"),
                    context.RequestAborted)
                    .ConfigureAwait(false);
            }
        }
    }
}
