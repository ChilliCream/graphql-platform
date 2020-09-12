using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Resolvers;
using static HotChocolate.Utilities.ThrowHelper;

namespace HotChocolate.Types.Pagination
{
    public class ConnectionMiddleware<TSource, TEntity>
    {
        private readonly QueryableConnectionResolver<TEntity> _connectionResolver =
            new QueryableConnectionResolver<TEntity>();
        private readonly FieldDelegate _next;
        private readonly int _defaultPageSize;
        private readonly int _maxPageSize;
        private readonly bool _withTotalCount;

        public ConnectionMiddleware(FieldDelegate next, ConnectionSettings settings)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _defaultPageSize = settings.DefaultPageSize ?? 10;
            _maxPageSize = settings.MaxPageSize ?? 50;
            _withTotalCount = settings.WithTotalCount ?? false;
        }

        public async Task InvokeAsync(
            IMiddlewareContext context,
            IConnectionResolver<TSource>? connectionResolver)
        {
            int? first = context.ArgumentValue<int?>(PaginationArguments.First);
            int? last = context.ArgumentValue<int?>(PaginationArguments.Last);

            if (first is null && last is null)
            {
                first = _defaultPageSize;
            }

            if (first > _maxPageSize || last > _maxPageSize)
            {
                throw ConnectionMiddleware_MaxPageSize();
            }

            await _next(context).ConfigureAwait(false);

            var arguments = new ConnectionArguments(
                first,
                last,
                context.ArgumentValue<string>(PaginationArguments.After),
                context.ArgumentValue<string>(PaginationArguments.Before));

            if (connectionResolver is not null && context.Result is TSource source)
            {
                context.Result = await connectionResolver.ResolveAsync(
                    context,
                    source,
                    arguments,
                    _withTotalCount,
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
