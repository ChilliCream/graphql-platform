using HotChocolate.AspNetCore.Instrumentation;
using HotChocolate.Serialization;
using Microsoft.AspNetCore.Http;
using static System.Net.HttpStatusCode;
using static HotChocolate.AspNetCore.Utilities.ErrorHelper;
using HttpRequestDelegate = Microsoft.AspNetCore.Http.RequestDelegate;

namespace HotChocolate.AspNetCore;

public sealed class HttpGetSemanticNonNullSchemaMiddleware : MiddlewareBase
{
    private static readonly AcceptMediaType[] s_mediaTypes =
    [
        new AcceptMediaType(
            ContentType.Types.Application,
            ContentType.SubTypes.GraphQLResponse,
            null,
            default)
    ];

    public HttpGetSemanticNonNullSchemaMiddleware(
        HttpRequestDelegate next,
        HttpRequestExecutorProxy executor,
        GraphQLServerOptions baseOptions)
        : base(next, executor, baseOptions)
    {
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.IsGetOrHeadMethod())
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

                    if (!context.Request.Query.TryGetValue("spec-version", out var specVersionValue))
                    {
                        await session.WriteSemanticNonNullSchemaAsync(context);
                        return;
                    }

                    var requestedSpecVersion = specVersionValue.ToString();

                    if (!GraphQLSpecVersions.TryParse(requestedSpecVersion, out var specVersion))
                    {
                        await session.WriteResultAsync(
                            context,
                            InvalidSpecVersion(requestedSpecVersion),
                            s_mediaTypes,
                            BadRequest);
                        return;
                    }

                    await session.WriteSemanticNonNullSchemaAsync(context, specVersion);
                }

                return;
            }
        }

        await NextAsync(context);
    }
}
