using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Resolvers;

namespace HotChocolate.Types.Relay
{
    public delegate IConnectionResolver ConnectionResolverFactory<in T>(
            IQueryable<T> source,
            PagingDetails pagingDetails);

    public class QueryableConnectionMiddleware<T>
    {
        private readonly FieldDelegate _next;
        private readonly ConnectionResolverFactory<T> _createConnectionResolver;

        public QueryableConnectionMiddleware(
            FieldDelegate next,
            ConnectionResolverFactory<T> createConnectionResolver)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _createConnectionResolver = createConnectionResolver
                ?? CreateConnectionResolver;
        }

        public async Task InvokeAsync(IMiddlewareContext context)
        {
            await _next(context).ConfigureAwait(false);

            var pagingDetails = new PagingDetails
            {
                First = context.ArgumentValue<int?>("first"),
                After = context.ArgumentValue<string>("after"),
                Last = context.ArgumentValue<int?>("last"),
                Before = context.ArgumentValue<string>("before"),
            };

            IQueryable<T> source = context.Result switch
            {
                IQueryable<T> q => q,
                IEnumerable<T> e => e.AsQueryable(),
                _ => null
            };

            if (source != null)
            {
                IConnectionResolver connectionResolver = _createConnectionResolver(
                    source, pagingDetails);

                context.Result = await connectionResolver
                    .ResolveAsync(context.RequestAborted)
                    .ConfigureAwait(false);
            }
        }

        private static IConnectionResolver CreateConnectionResolver(
            IQueryable<T> source, PagingDetails pagingDetails)
        {
            return new QueryableConnectionResolver<T>(
                source, pagingDetails);
        }
    }
}
