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
        private readonly FilterMiddlewareContext _contextData;

        public FilterMiddleware(
            FieldDelegate next,
            FilterMiddlewareContext contextData,
            ITypeConversion converter)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _converter = converter ?? TypeConversion.Default;
            _contextData = contextData ??
                throw new ArgumentNullException(nameof(contextData));
        }

        public Task InvokeAsync(IMiddlewareContext context) =>
            _contextData.Convention.ApplyFilter<T>(_next, _converter, context);
    }
}
