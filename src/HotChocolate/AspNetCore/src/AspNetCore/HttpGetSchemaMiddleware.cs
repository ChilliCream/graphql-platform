using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using HotChocolate.AspNetCore.Utilities;
using HotChocolate.Execution;
using HttpRequestDelegate = Microsoft.AspNetCore.Http.RequestDelegate;

namespace HotChocolate.AspNetCore
{
    public class HttpGetSchemaMiddleware : MiddlewareBase
    {

        public HttpGetSchemaMiddleware(
            HttpRequestDelegate next,
            IRequestExecutorResolver executorResolver,
            IHttpResultSerializer resultSerializer,
            NameString schemaName)
            : base(next, executorResolver, resultSerializer, schemaName)
        {
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (HttpMethods.IsGet(context.Request.Method) &&
                context.Request.Query.ContainsKey("SDL") &&
                (context.GetGraphQLServerOptions()?.EnableSchemaRequests ?? true))
            {
                await HandleRequestAsync(context);
            }
            else
            {
                // if the request is not a get request or if the content type is not correct
                // we will just invoke the next middleware and do nothing.
                await NextAsync(context);
            }
        }

        private async Task HandleRequestAsync(HttpContext context)
        {
            IRequestExecutor requestExecutor = await GetExecutorAsync(context.RequestAborted);

            string fileName =
                requestExecutor.Schema.Name.IsEmpty ||
                requestExecutor.Schema.Name.Equals(Schema.DefaultName)
                    ? "schema.graphql"
                    : requestExecutor.Schema.Name + ".schema.graphql";

            context.Response.ContentType = ContentType.GraphQL;
            context.Response.Headers.Add(
                "Content-Disposition",
                new[] { $"attachment; filename=\"{fileName}\"" });

            await using var memoryStream = new MemoryStream();
            await using var streamWriter = new StreamWriter(memoryStream);

            SchemaSerializer.Serialize(requestExecutor.Schema, streamWriter);
            await streamWriter.FlushAsync().ConfigureAwait(false);

            memoryStream.Seek(0, SeekOrigin.Begin);
            await memoryStream.CopyToAsync(context.Response.Body).ConfigureAwait(false);
        }
    }
}
