using System;
using System.IO;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Server;

#if ASPNETCLASSIC
using Microsoft.Owin;
using HttpContext = Microsoft.Owin.IOwinContext;
using HttpResponse = Microsoft.Owin.IOwinResponse;
using RequestDelegate = Microsoft.Owin.OwinMiddleware;
#else
using Microsoft.AspNetCore.Http;
#endif

#if ASPNETCLASSIC
namespace HotChocolate.AspNetClassic
#else
namespace HotChocolate.AspNetCore
#endif
{
    internal sealed class HttpGetSchemaMiddleware
#if ASPNETCLASSIC
        : RequestDelegate
#endif
    {
        private readonly PathString _path;
        private readonly IQueryExecutor _queryExecutor;

        public HttpGetSchemaMiddleware(
            RequestDelegate next,
            IHttpGetSchemaMiddlewareOptions options,
            IQueryExecutor queryExecutor)
#if ASPNETCLASSIC
            : base(next)
#endif
        {
#if !ASPNETCLASSIC
            Next = next;
#endif
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _queryExecutor = queryExecutor
                ?? throw new ArgumentNullException(nameof(queryExecutor));
            _path = options.Path;
        }

#if !ASPNETCLASSIC
        internal RequestDelegate Next { get; }
#endif

#if ASPNETCLASSIC
        public override async Task Invoke(HttpContext context)
#else
        public async Task InvokeAsync(HttpContext context)
#endif
        {
            if (context.Request.Method.Equals(
                HttpMethods.Get,
                StringComparison.Ordinal)
                && context.IsValidPath(_path))
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
