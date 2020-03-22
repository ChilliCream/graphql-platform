using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Resolvers;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Selections
{
    public class SelectionMiddleware<T>
    {
        private readonly FieldDelegate _next;
        private readonly SelectionMiddlewareContext _context;
        private readonly ITypeConversion _converter;
        public SelectionMiddleware(
            FieldDelegate next,
            SelectionMiddlewareContext context,
            ITypeConversion converter)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _context = context ?? throw new ArgumentNullException(nameof(context));
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

            var visitor = new SelectionVisitor(context, _converter, _context);
            visitor.Accept(context.Field);
            context.Result = source.Select(visitor.Project<T>());
        }
    }
}
