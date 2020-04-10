using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types.Relay;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Filters
{
    public class QueryableFilterMiddleware<T>
    {
        private readonly FieldDelegate _next;
        private readonly ITypeConversion _converter;
        private readonly FilterMiddlewareContext _contextData;

        public QueryableFilterMiddleware(
            FieldDelegate next,
            FilterMiddlewareContext contextData,
            ITypeConversion converter)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _converter = converter ?? TypeConversion.Default;
            _contextData = contextData
                 ?? throw new ArgumentNullException(nameof(contextData));
        }

        public async Task InvokeAsync(IMiddlewareContext context)
        {
            await _contextData.Convention.ApplyFilter<T>(_next, _converter, context)
                .ConfigureAwait(false);
        }
    }
}
