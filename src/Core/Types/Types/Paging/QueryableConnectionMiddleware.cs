using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Paging
{
    public class QueryableConnectionMiddleware<T>
    {
        private readonly FieldDelegate _next;

        public QueryableConnectionMiddleware(FieldDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
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
                var factory = new QueryableConnectionFactory<T>(
                    source,
                    pagingDetails,
                    HasNextPageRequested(context.FieldSelection),
                    HasPreviousPageRequested(context.FieldSelection));
                context.Result = await factory.CreateAsync(
                    context.RequestAborted);
            }
        }

        private bool HasNextPageRequested(FieldNode fieldSelection) =>
            IsPageInfoFieldSelected(fieldSelection, "hasNextPage");

        private bool HasPreviousPageRequested(FieldNode fieldSelection) =>
            IsPageInfoFieldSelected(fieldSelection, "hasPreviousPage");


        private bool IsPageInfoFieldSelected(
            FieldNode fieldSelection,
            string fieldName)
        {
            if (fieldSelection.SelectionSet == null)
            {
                return false;
            }

            FieldNode pageInfo = fieldSelection.SelectionSet.Selections
                .OfType<FieldNode>().FirstOrDefault(
                    t => t.Name.Value.EqualsOrdinal("pageInfo"));

            if (pageInfo == null)
            {
                return false;
            }

            return fieldSelection.SelectionSet.Selections
                .OfType<FieldNode>().Any(
                    t => t.Name.Value.EqualsOrdinal(fieldName));
        }
    }
}
