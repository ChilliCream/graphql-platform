using System;
using System.Threading.Tasks;
using HotChocolate.Resolvers;

namespace HotChocolate.Data.Filters
{
    public class FilterMiddleware
    {
        private readonly FieldDelegate _execute;

        public FilterMiddleware(
            FieldDelegate next,
            FieldMiddleware filterExecution)
        {
            if (next is null)
            {
                throw new ArgumentNullException(nameof(next));
            }

            if (filterExecution is null)
            {
                throw new ArgumentNullException(nameof(filterExecution));
            }
        }

        public Task InvokeAsync(IMiddlewareContext context) =>
            _contextData.Convention.ExecuteAsync<TEntityType>(_next, context);
    }
}
