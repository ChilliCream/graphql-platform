using System;
using System.IO;
using System.Threading.Tasks;
using HotChocolate.Execution;

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
    internal sealed class SchemaMiddleware
#if ASPNETCLASSIC
        : RequestDelegate
#endif
    {
        private readonly PathString _path;
        private readonly IQueryExecutor _queryExecutor;

        public SchemaMiddleware(
           RequestDelegate next,
           IQueryExecutor queryExecutor,
           QueryMiddlewareOptions options)
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
            _path = options.Path.Add(new PathString("/schema"));
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
            if (context.Request.Method.Equals(HttpMethods.Get,
                StringComparison.Ordinal) && context.IsValidPath(_path))
            {
                context.Response.ContentType = "application/graphql";
                context.Response.Headers.Add(
                    "Content-Disposition",
                    new[] { "attachment; filename=\"schema.graphql\"" });

                using (var streamWriter = new StreamWriter(
                   context.Response.Body))
                {
                    SchemaSerializer.Serialize(
                        _queryExecutor.Schema,
                        streamWriter);

                    await streamWriter.FlushAsync();
                }
            }
            else
            {
                await Next.Invoke(context);
            }
        }
    }
}
