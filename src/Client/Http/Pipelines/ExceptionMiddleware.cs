using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace StrawberryShake.Http.Pipelines
{
    public class ExceptionMiddleware
    {
        private readonly OperationDelegate _next;

        public ExceptionMiddleware(OperationDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public async Task InvokeAsync(IHttpOperationContext context)
        {
            try
            {
                await _next(context);
            }
            catch (HttpRequestException ex)
            {
                IError error = ErrorBuilder.FromException(ex)
                    .SetCode(ErrorCodes.HttpError)
                    .Build();

                context.Result = OperationResultBuilder.New()
                    .AddError(error)
                    .Build();
            }
            catch (Exception ex)
            {
                ErrorBuilder.
            }
        }
    }
}
