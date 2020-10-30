using System;
using System.Threading.Tasks;
using HotChocolate.Resolvers;

namespace HotChocolate.Data.Projections
{
    public sealed class FirstOrDefaultMiddleware<T>
    {
        public const string ContextKey = nameof(FirstOrDefaultMiddleware<object>);

        private readonly FieldDelegate _next;

        public FirstOrDefaultMiddleware(FieldDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public async Task InvokeAsync(IMiddlewareContext context)
        {
            await _next(context).ConfigureAwait(false);
            if (context.Result is IFirstOrDefaultExecutable ae)
            {
                context.Result = ae.AddFirstOrDefault();
            }
            else
            {
                context.Result = await FirstOrDefaultExecutor
                    .ExecuteAsync<T>(context.Result, context.RequestAborted)
                    .ConfigureAwait(false);
            }
        }
    }
}
