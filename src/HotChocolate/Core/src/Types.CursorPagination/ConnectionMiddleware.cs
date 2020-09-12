using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Resolvers;

namespace HotChocolate.Types.Relay
{
    public class ConnectionMiddleware<TSource, TEntity>
    {
        private readonly QueryableConnectionResolver<TEntity> _connectionResolver =
            new QueryableConnectionResolver<TEntity>();
        private readonly FieldDelegate _next;

        public ConnectionMiddleware(FieldDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public async Task InvokeAsync(
            IMiddlewareContext context,
            IConnectionResolver<TSource>? connectionResolver)
        {
            await _next(context).ConfigureAwait(false);

            var arguments = new ConnectionArguments(
                context.ArgumentValue<int?>(PaginationArguments.First),
                context.ArgumentValue<int?>(PaginationArguments.Last),
                context.ArgumentValue<string>(PaginationArguments.After),
                context.ArgumentValue<string>(PaginationArguments.Before));

            if (connectionResolver is not null && context.Result is TSource source)
            {
                context.Result = await connectionResolver.ResolveAsync(
                    context,
                    source,
                    arguments,
                    true, // where should we store this?
                    context.RequestAborted)
                    .ConfigureAwait(false);
            }
            else if (context.Result is IQueryable<TEntity> queryable)
            {
                context.Result = await _connectionResolver.ResolveAsync(
                    context,
                    queryable,
                    arguments,
                    true, // where should we store this?
                    context.RequestAborted)
                    .ConfigureAwait(false);
            }
            else if (context.Result is IEnumerable<TEntity> enumerable)
            {
                context.Result = await _connectionResolver.ResolveAsync(
                    context,
                    enumerable.AsQueryable(),
                    arguments,
                    true, // where should we store this?
                    context.RequestAborted)
                    .ConfigureAwait(false);
            }
        }
    }
}
