using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Resolvers;

namespace HotChocolate.Types.Sorting
{
    [Obsolete("Use HotChocolate.Data.")]
    public class QueryableSortMiddleware<T>
    {
        private readonly SortMiddlewareContext _contextData;
        private readonly FieldDelegate _next;

        public QueryableSortMiddleware(
            FieldDelegate next,
            SortMiddlewareContext contextData)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _contextData = contextData
                 ?? throw new ArgumentNullException(nameof(contextData));
        }

        public async Task InvokeAsync(IMiddlewareContext context)
        {
            await _next(context).ConfigureAwait(false);

            IValueNode sortArgument = context.ArgumentLiteral<IValueNode>(_contextData.ArgumentName);

            if (sortArgument is NullValueNode)
            {
                return;
            }

            IQueryable<T>? source = null;

            if (context.Result is IQueryable<T> q)
            {
                source = q;
            }
            else if (context.Result is IEnumerable<T> e)
            {
                source = e.AsQueryable();
            }

            if (source is not null &&
                context.Field.Arguments[_contextData.ArgumentName].Type is InputObjectType iot &&
                iot is ISortInputType { EntityType: not null! } fit)
            {
                var visitorCtx = new QueryableSortVisitorContext(
                    iot,
                    fit.EntityType,
                    source is EnumerableQuery);

                QueryableSortVisitor.Default.Visit(sortArgument, visitorCtx);

                source = visitorCtx.Sort(source);
                context.Result = source;
            }
        }
    }
}
