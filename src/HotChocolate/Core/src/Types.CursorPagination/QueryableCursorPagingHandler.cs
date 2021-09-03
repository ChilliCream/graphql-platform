﻿using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Resolvers;

namespace HotChocolate.Types.Pagination
{
    internal class QueryableCursorPagingHandler<TEntity> : CursorPagingHandler
    {
        private readonly QueryableCursorPagination<TEntity> _pagination =
            QueryableCursorPagination<TEntity>.Instance;

        public QueryableCursorPagingHandler(PagingOptions options)
            : base(options)
        {
        }

        protected override ValueTask<Connection> SliceAsync(
            IResolverContext context,
            object source,
            CursorPagingArguments arguments)
        {
            CancellationToken ct = context.RequestAborted;
            return source switch
            {
                IQueryable<TEntity> q => ResolveAsync(q, arguments, ct),
                IEnumerable<TEntity> e => ResolveAsync(e.AsQueryable(), arguments, ct),
                IExecutable<TEntity> ex => SliceAsync(context, ex.Source, arguments),
                _ => throw new GraphQLException("Cannot handle the specified data source.")
            };
        }

        private async ValueTask<Connection> ResolveAsync(
            IQueryable<TEntity> source,
            CursorPagingArguments arguments = default,
            CancellationToken cancellationToken = default)
            => await _pagination
                .ApplyPaginationAsync(source, arguments, cancellationToken)
                .ConfigureAwait(false);
    }
}
