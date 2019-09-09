using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StrawberryShake.Http.Middleware
{
    public class ParseSingleResultMiddleware
    {
        private readonly OperationDelegate _next;
        private readonly IReadOnlyDictionary<Type, IResultParser> _resultParsers;

        public ParseSingleResultMiddleware(
            OperationDelegate next,
            IEnumerable<IResultParser> resultParsers)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _resultParsers = resultParsers.ToDictionary();
        }

        public async Task InvokeAsync(IHttpOperationContext context)
        {
            if (context.HttpResponse != null && context.Result is null)
            {
                context.HttpResponse.EnsureSuccessStatusCode();

                // TOOD : throw error if not exists
                IResultParser resultParser = _resultParsers[context.Operation.ResultType];

                using (var stream = await context.HttpResponse.Content.ReadAsStreamAsync()
                    .ConfigureAwait(false))
                {
                    context.Result = await resultParser.ParseAsync(
                        stream, context.RequestAborted)
                        .ConfigureAwait(false);
                }
            }

            await _next(context);
        }
    }
}
