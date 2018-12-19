using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Paging
{
    public delegate IConnectionResolver ConnectionResolverFactory<T>(
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
            await _next(context);

            var pagingDetails = new PagingDetails
            {
                First = context.Argument<int?>("first"),
                After = context.Argument<string>("after"),
                Last = context.Argument<int?>("last"),
                Before = context.Argument<string>("before"),
            };

            IQueryable<T> source = null;

            if (context.Result is PageableData<T> p)
            {
                source = p.Source;
                pagingDetails.Properties = p.Properties;
            }

            if (context.Result is IQueryable<T> q)
            {
                source = q;
            }

            if (context.Result is IEnumerable<T> e)
            {
                source = e.AsQueryable();
            }

            if (source != null)
            {
                var connectionResolver = _createConnectionResolver(
                    source, pagingDetails);

                context.Result = await connectionResolver.ResolveAsync(
                    context.RequestAborted);
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
