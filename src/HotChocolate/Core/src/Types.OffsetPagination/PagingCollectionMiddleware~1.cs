using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Resolvers;

namespace HotChocolate.Types.OffsetPaging
{
    public class PagingCollectionMiddleware<TClrType>
    {
        private readonly FieldDelegate _next;

        public PagingCollectionMiddleware(FieldDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(IMiddlewareContext context)
        {
            await _next(context).ConfigureAwait(false);

            IQueryable<TClrType> source = context.Result switch
            {
                IQueryable<TClrType> queryable => queryable,
                IEnumerable<TClrType> enumerable => enumerable.AsQueryable(),
                _ => null
            };

            if (source != null)
            {
                int? skip = context.Argument<int?>("skip");
                int? take = context.Argument<int?>("take");

                var totalCount = source.Count();

                IQueryable<TClrType> slice = source;

                if (skip != null)
                    slice = slice.Skip(skip.Value);
                if (take != null)
                    slice = slice.Take(take.Value);

                context.Result = new CollectionSlice<TClrType>(slice.ToList(), totalCount);
            }
        }
    }
}
