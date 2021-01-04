using System;
using System.Net.Http;
using System.Threading.Tasks;
using StrawberryShake.Transport;

namespace StrawberryShake.Http.Pipelines
{
    public class CreateStandardRequestMiddleware
    {
        private static readonly OperationFormatterOptions _options =
            OperationFormatterOptions.Default;
        private readonly OperationDelegate<IHttpOperationContext> _next;

        public CreateStandardRequestMiddleware(
            OperationDelegate<IHttpOperationContext> next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public async Task InvokeAsync(IHttpOperationContext context)
        {
            if (context.HttpRequest is null)
            {
                context.HttpRequest = new HttpRequestMessage(
                    HttpMethod.Post, context.Client.BaseAddress);

                context.OperationFormatter.Serialize(
                    context.Operation,
                    context.RequestWriter,
                    _options);

                context.HttpRequest.Content = context.RequestWriter.ToByteArrayContent();
                context.HttpRequest.Content.Headers.Add(
                    WellKnownHeaders.ContentTypeJson.Name,
                    WellKnownHeaders.ContentTypeJson.Value);
            }

            await _next(context).ConfigureAwait(false);
        }
    }
}
