using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Resolvers;
using HotChocolate.Types.Selections;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Selections
{
    public class SelectionMiddleware<T>
    {
        private readonly FieldDelegate _next;

        private readonly ITypeConversion _converter;
        public SelectionMiddleware(
            FieldDelegate next,
            ITypeConversion converter)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _converter = converter ?? TypeConversion.Default;
        }

        public async Task InvokeAsync(IMiddlewareContext context)
        {
            await _next(context).ConfigureAwait(false);

            IQueryable<T> source = null;

            if (context.Result is IQueryable<T> q)
            {
                source = q;
            }
            else if (context.Result is IEnumerable<T> e)
            {
                source = e.AsQueryable();
            }

            var visitor = new SelectionVisitor(context, _converter);
            visitor.Accept(context.Field);
            context.Result = source.Select(visitor.Project<T>());
        }
    }
}
