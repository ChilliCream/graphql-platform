using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Resolvers;
using static HotChocolate.Utilities.ThrowHelper;

#nullable enable

namespace HotChocolate.Types.Pagination
{
    public class PagingMiddleware
    {
        private readonly FieldDelegate _next;
        private readonly IPagingHandler _pagingHandler;

        public PagingMiddleware(FieldDelegate next, IPagingHandler pagingHandler)
        {
            _next = next ??
                throw new ArgumentNullException(nameof(next));
            _pagingHandler = pagingHandler ??
                throw new ArgumentNullException(nameof(pagingHandler));
        }

        public async Task InvokeAsync(IMiddlewareContext context)
        {
            _pagingHandler.ValidateContext(context);

            await _next(context).ConfigureAwait(false);

            if (context.Result is not null)
            {
                context.Result =
                    await _pagingHandler.SliceAsync(context, context.Result).ConfigureAwait(false);
            }
        }
    }
}
