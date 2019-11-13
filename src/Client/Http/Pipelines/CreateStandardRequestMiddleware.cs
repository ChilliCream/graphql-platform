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
        private readonly OperationDelegate _next;
        private readonly IOperationFormatter _formatter;

        public CreateStandardRequestMiddleware(
            OperationDelegate next,
            IOperationFormatter formatter)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
        }

        public async Task InvokeAsync(IHttpOperationContext context)
        {
            if (context.HttpRequest is null)
            {
                context.HttpRequest = new HttpRequestMessage(
                    HttpMethod.Post, context.Client.BaseAddress);

                _formatter.Serialize(context.Operation, context.MessageWriter, _options);

                context.HttpRequest.Content = context.MessageWriter.ToByteArrayContent();
                context.HttpRequest.Content.Headers.Add(
                    WellKnownHeaders.ContentTypeJson.Name,
                    WellKnownHeaders.ContentTypeJson.Value);
            }

            await _next(context);
        }
    }
}
