using HotChocolate.AspNetCore.Instrumentation;
using Microsoft.AspNetCore.Http;
using HttpRequestDelegate = Microsoft.AspNetCore.Http.RequestDelegate;

namespace HotChocolate.AspNetCore;

public sealed class HttpGetSemanticNonNullSchemaMiddleware : MiddlewareBase
{
    public HttpGetSemanticNonNullSchemaMiddleware(
        HttpRequestDelegate next,
        HttpRequestExecutorProxy executor,
        GraphQLServerOptions baseOptions)
        : base(next, executor, baseOptions)
    {
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (HttpMethods.IsGet(context.Request.Method))
        {
            var session = await Executor.GetOrCreateSessionAsync(context.RequestAborted);
            var options = GetOptions(context);

            if (options.EnableSchemaRequests)
            {
                using (session.DiagnosticEvents.ExecuteHttpRequest(context, HttpRequestKind.HttpGetSemanticNonNullSchema))
                {
                    if (!options.EnableSchemaFileSupport)
                    {
                        context.Response.StatusCode = 404;
                        return;
                    }

                    await session.WriteSemanticNonNullSchemaAsync(context);
                }

                return;
            }
        }

        await NextAsync(context);
    }
}
