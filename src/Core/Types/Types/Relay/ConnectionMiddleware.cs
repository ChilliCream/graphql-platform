using System;
using System.Threading.Tasks;
using HotChocolate.Resolvers;

namespace HotChocolate.Types.Relay
{
    public class ConnectionMiddleware<TSource>
    {
        private readonly FieldDelegate _next;

        public ConnectionMiddleware(FieldDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public async Task InvokeAsync(
            IMiddlewareContext context,
            IConnectionResolver<TSource> connectionResolver)
        {
            await _next(context).ConfigureAwait(false);

            var arguments = new ConnectionArguments(
                context.Argument<int?>("first"),
                context.Argument<int?>("last"),
                context.Argument<string>("after"),
                context.Argument<string>("before"));

            if (context.Result is IConnectionResolver localConnectionResolver)
            {
                context.Result = localConnectionResolver.ResolveAsync(
                    context,
                    default,
                    arguments,
                    true, // where should we store this?
                    context.RequestAborted)
                    .ConfigureAwait(false);
            }

            if (connectionResolver is { } && context.Result is TSource item)
            {
                context.Result = await connectionResolver.ResolveAsync(
                    context,
                    default,
                    arguments,
                    true, // where should we store this?
                    context.RequestAborted)
                    .ConfigureAwait(false);
            }
        }
    }
}
