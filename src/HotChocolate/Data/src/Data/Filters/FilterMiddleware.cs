using System;
using System.Threading.Tasks;
using HotChocolate.Resolvers;

namespace HotChocolate.Data.Filters
{
    public class FilterMiddleware<TEntityType>
    {
        private readonly FieldDelegate _next;
        private readonly FilterMiddlewareContext _contextData;

        public FilterMiddleware(
            FieldDelegate next,
            FilterMiddlewareContext contextData)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _contextData = contextData ?? throw new ArgumentNullException(nameof(next));
        }

        public Task InvokeAsync(IMiddlewareContext context) =>
            _contextData.Convention.ExecuteAsync<TEntityType>(_next, context);
    }
}