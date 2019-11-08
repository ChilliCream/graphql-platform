using System;
using System.Buffers;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace StrawberryShake.Http.Pipelines
{
    public class CreateStandardRequestMiddleware
    {
        private static readonly OperationSerializerOptions _options =
            OperationSerializerOptions.Default;
        private readonly OperationDelegate _next;
        private readonly IOperationSerializer _serializer;

        public CreateStandardRequestMiddleware(
            OperationDelegate next,
            IOperationSerializer serializer)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        public async Task InvokeAsync(IHttpOperationContext context)
        {
            if (context.HttpRequest is null)
            {
                context.HttpRequest = new HttpRequestMessage(
                    HttpMethod.Post, context.Client.BaseAddress);

                _serializer.Serialize(context.Operation, context.MessageWriter, _options);

                context.HttpRequest.Content = context.MessageWriter.ToByteArrayContent();
                context.HttpRequest.Content.Headers.Add(
                    WellKnownHeaders.ContentTypeJson.Name,
                    WellKnownHeaders.ContentTypeJson.Value);
            }

            await _next(context);
        }
    }
}
