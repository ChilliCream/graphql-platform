using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StrawberryShake.Http.Pipelines
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
            if (context.HttpResponse != null && !context.Result.IsDataOrErrorModified)
            {
                context.HttpResponse.EnsureSuccessStatusCode();

                if (!_resultParsers.TryGetValue(
                    context.Operation.ResultType,
                    out IResultParser? resultParser))
                {
                    context.Result.AddError(
                        ErrorBuilder.New()
                            .SetMessage(
                                "There is no result parser registered for " +
                                $"`{context.Operation.ResultType.FullName}`.")
                            .SetCode(ErrorCodes.NoResultParser)
                            .Build());
                }
                else
                {
                    using (var stream = await context.HttpResponse.Content.ReadAsStreamAsync()
                        .ConfigureAwait(false))
                    {
                        await resultParser.ParseAsync(
                            stream, context.Result, context.RequestAborted)
                            .ConfigureAwait(false);
                    }
                }
            }

            await _next(context);
        }
    }
}
