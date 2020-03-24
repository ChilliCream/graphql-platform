using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types.Relay;

namespace HotChocolate.Types.Sorting
{
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

            IValueNode sortArgument = context.Argument<IValueNode>(_contextData.ArgumentName);

            if (sortArgument is null || sortArgument is NullValueNode)
            {
                return;
            }

            IQueryable<T> source = null;

            if (context.Result is IQueryable<T> q)
            {
                source = q;
            }
            else if (context.Result is IEnumerable<T> e)
            {
                source = e.AsQueryable();
            }

            if (context.Result is PageableData<T> p)
            {
                source = p.Source;
            }
            else
            {
                p = null;
            }

            if (source != null
                && context.Field.Arguments[_contextData.ArgumentName].Type is InputObjectType iot
                && iot is ISortInputType fit)
            {
                var visitorCtx = new QueryableSortVisitorContext(
                    iot,
                    fit.EntityType,
                    source is EnumerableQuery);

                QueryableSortVisitor.Default.Visit(sortArgument, visitorCtx);

                source = visitorCtx.Sort(source);
                context.Result = p is null
                    ? (object)source
                    : new PageableData<T>(source, p.Properties);
            }
        }
    }
}
