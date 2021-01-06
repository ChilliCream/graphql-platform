using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using HotChocolate.AspNetCore.Serialization;
using HotChocolate.Execution;
using HttpRequestDelegate = Microsoft.AspNetCore.Http.RequestDelegate;
using Microsoft.Net.Http.Headers;
using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;

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
            var isHeadHttpMethod = HttpMethods.IsHead(context.Request.Method);

            if ((isHeadHttpMethod || HttpMethods.IsGet(context.Request.Method)) &&
                context.Request.Query.ContainsKey("SDL") &&
                (context.GetGraphQLServerOptions()?.EnableSchemaRequests ?? true))
            {
                await HandleRequestAsync(context, isHeadHttpMethod);
            }
            else
            {
                // if the request is not supported just invoke the next middleware
                await NextAsync(context);
            }
        }

        private async Task HandleRequestAsync(HttpContext context, bool isHeadHttpMethod)
        {
            IRequestExecutor requestExecutor = await GetExecutorAsync(context.RequestAborted);

            if (!isHeadHttpMethod)
            {
                string fileName =
                    requestExecutor.Schema.Name.IsEmpty ||
                    requestExecutor.Schema.Name.Equals(Schema.DefaultName)
                        ? "schema.graphql"
                        : requestExecutor.Schema.Name + ".schema.graphql";

                context.Response.ContentType = ContentType.GraphQL;
                context.Response.Headers.Add(
                    HeaderNames.ContentDisposition,
                    new[] { $"attachment; filename=\"{fileName}\"" });

                await SchemaSerializer.SerializeAsync(
                    requestExecutor.Schema,
                    context.Response.Body,
                    indented: true,
                    context.RequestAborted)
                    .ConfigureAwait(false);
            }

            if (context.GetGraphQLServerOptions()?.EnableSchemaRequestsChecksumResponseHeader ?? true)
            {
                await SetSchemaChecksumHeaderAsync(context, requestExecutor, context.Response.Body.Length);
            }
        }

        private async ValueTask SetSchemaChecksumHeaderAsync(
            HttpContext context,
            IRequestExecutor requestExecutor,
            long streamLength)
        {
            using var ms = new MemoryStream((int)streamLength);

            await SchemaSerializer.SerializeAsync(
                requestExecutor.Schema,
                ms,
                indented: true,
                context.RequestAborted)
                .ConfigureAwait(false);

            string checksum = "";

            using (var algo = SHA1.Create())
            {
                ms.Position = 0;
                byte[] bytes = algo.ComputeHash(ms);
                checksum = $"\"{WebEncoders.Base64UrlEncode(bytes)}\"";
            }

            context.Response.Headers.Add(
                HttpHeaderKeys.HotChocolateSchemaChecksum,
                checksum);
        }
    }
}
