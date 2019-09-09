using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace StrawberryShake.Http.Pipeline
{
    public class CreateStandardRequestMiddleware
    {
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

                using (var stream = new MemoryStream())
                {
                    await _serializer.SerializeAsync(
                        context.Operation, null, true, stream)
                        .ConfigureAwait(false);
                    context.HttpRequest.Content = new ByteArrayContent(
                        stream.ToArray());
                    context.HttpRequest.Content.Headers.Add(
                        "Content-Type", "application/json");
                }
            }

            await _next(context);
        }
    }
}
