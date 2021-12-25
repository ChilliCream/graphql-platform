using System.IO;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Serialization;
using HotChocolate.Execution;
using Microsoft.AspNetCore.Http;
using HttpRequestDelegate = Microsoft.AspNetCore.Http.RequestDelegate;

namespace HotChocolate.AspNetCore;

public class HttpGetSchemaMiddleware : MiddlewareBase
{
    private readonly MiddlewareRoutingType _routing;

    public HttpGetSchemaMiddleware(
        HttpRequestDelegate next,
        IRequestExecutorResolver executorResolver,
        IHttpResultSerializer resultSerializer,
        NameString schemaName,
        MiddlewareRoutingType routing)
        : base(next, executorResolver, resultSerializer, schemaName)
    {
        _routing = routing;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var handle = _routing == MiddlewareRoutingType.Integrated
            ? HttpMethods.IsGet(context.Request.Method) &&
              context.Request.Query.ContainsKey("SDL") &&
              (context.GetGraphQLServerOptions()?.EnableSchemaRequests ?? true)
            : HttpMethods.IsGet(context.Request.Method) &&
              (context.GetGraphQLServerOptions()?.EnableSchemaRequests ?? true);

        if (handle)
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

        await SchemaSerializer.SerializeAsync(
            requestExecutor.Schema,
            context.Response.Body,
            indented: true,
            context.RequestAborted)
            .ConfigureAwait(false);
    }
}
