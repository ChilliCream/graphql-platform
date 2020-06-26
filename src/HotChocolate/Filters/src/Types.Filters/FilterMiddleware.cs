using System;
using System.Threading.Tasks;
using HotChocolate.Resolvers;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Filters
{
    public class FilterMiddleware<T>
    {
        private readonly FieldDelegate _next;
        private readonly ITypeConversion _converter;
        private readonly FilterMiddlewareContext _filterContext;

        public FilterMiddleware(
            FieldDelegate next,
            FilterMiddlewareContext filterContext,
            ITypeConversion converter)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _filterContext = filterContext ?? throw new ArgumentNullException(nameof(filterContext));
            _converter = converter ?? TypeConversion.Default;
        }

        public Task InvokeAsync(IMiddlewareContext context) =>
            _filterContext.Convention.ApplyFilterAsync<T>(_next, _converter, context);
    }
}
