using System;
using System.Threading.Tasks;
using HotChocolate.Resolvers;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Sorting
{
    public class SortMiddleware<T>
    {
        private readonly SortMiddlewareContext _contextData;
        private readonly FieldDelegate _next;
        private readonly ITypeConversion _converter;

        public SortMiddleware(
            FieldDelegate next,
            ITypeConversion converter,
            SortMiddlewareContext contextData)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _contextData = contextData
                 ?? throw new ArgumentNullException(nameof(contextData));
            _converter = converter ?? TypeConversion.Default;
        }

        public async Task InvokeAsync(IMiddlewareContext context)
        {
            await _contextData.Convention.ApplySorting<T>(_next, _converter, context);
        }
    }
}
