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
        private readonly FieldDelegate _next;

        public QueryableSortMiddleware(
            FieldDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public async Task InvokeAsync(IMiddlewareContext context)
        {
            await _next(context).ConfigureAwait(false);

            IValueNode sortArgument = context.Argument<IValueNode>(
                SortObjectFieldDescriptorExtensions.OrderByArgumentName);

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

            if (source != null
                && context.Field
                    .Arguments[SortObjectFieldDescriptorExtensions.OrderByArgumentName]
                    .Type is InputObjectType iot
                && iot is ISortInputType fit)
            {
                var visitor = new QueryableSortVisitor(
                    iot,
                    fit.EntityType);
                sortArgument.Accept(visitor);

                source = visitor.Sort(source);
                context.Result = source;
            }
        }
    }
}
