using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using HotChocolate.Execution;
using HotChocolate.Server;

namespace HotChocolate.AspNetCore
{
    public sealed class HttpGetSchemaMiddleware
    {
        private readonly IQueryExecutor _queryExecutor;

        public HttpGetSchemaMiddleware(
            RequestDelegate next,
            IQueryExecutor queryExecutor)
        {
            Next = next;
            _queryExecutor = queryExecutor
                ?? throw new ArgumentNullException(nameof(queryExecutor));
        }

        internal RequestDelegate Next { get; }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Method.Equals(
                HttpMethods.Get,
                StringComparison.Ordinal))
            {
                context.Response.ContentType = ContentType.GraphQL;
                context.Response.Headers.Add(
                    "Content-Disposition",
                    new[] { "attachment; filename=\"schema.graphql\"" });

                using (var memoryStream = new MemoryStream())
                {
                    using (var streamWriter = new StreamWriter(memoryStream))
                    {
                        SchemaSerializer.Serialize(
                            _queryExecutor.Schema,
                            streamWriter);
                        await streamWriter.FlushAsync().ConfigureAwait(false);

                        memoryStream.Seek(0, SeekOrigin.Begin);
                        await memoryStream.CopyToAsync(context.Response.Body).ConfigureAwait(false);
                    }
                }
            }
            else
            {
                await Next.Invoke(context).ConfigureAwait(false);
            }
        }
    }
}
