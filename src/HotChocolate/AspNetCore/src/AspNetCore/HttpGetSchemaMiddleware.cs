using Microsoft.AspNetCore.Http;
using HotChocolate.AspNetCore.Instrumentation;
using HotChocolate.AspNetCore.Serialization;
using HttpRequestDelegate = Microsoft.AspNetCore.Http.RequestDelegate;
using static System.Net.HttpStatusCode;
using static HotChocolate.AspNetCore.ErrorHelper;
using static HotChocolate.SchemaSerializer;
namespace HotChocolate.AspNetCore;

public sealed class HttpGetSchemaMiddleware : MiddlewareBase
{
    private readonly MiddlewareRoutingType _routing;
    private readonly IServerDiagnosticEvents _diagnosticEvents;

    public HttpGetSchemaMiddleware(
        HttpRequestDelegate next,
        IRequestExecutorResolver executorResolver,
        IHttpResultSerializer resultSerializer,
        IServerDiagnosticEvents diagnosticEvents,
        NameString schemaName,
        MiddlewareRoutingType routing)
        : base(next, executorResolver, resultSerializer, schemaName)
    {
        _diagnosticEvents = diagnosticEvents ??
            throw new ArgumentNullException(nameof(diagnosticEvents));
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
            if (!IsDefaultSchema)
            {
                context.Items[WellKnownContextData.SchemaName] = SchemaName.Value;
            }

            using (_diagnosticEvents.ExecuteHttpRequest(context, HttpRequestKind.HttpGetSchema))
            {
                await HandleRequestAsync(context);
            }
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
        ISchema schema = await GetSchemaAsync(context.RequestAborted);
        context.Items[WellKnownContextData.Schema] = schema;

        bool indent =
            !(context.Request.Query.ContainsKey("indentation") &&
                string.Equals(
                    context.Request.Query["indentation"].FirstOrDefault(),
                    "none",
                    StringComparison.OrdinalIgnoreCase));

        if (context.Request.Query.TryGetValue("types", out Microsoft.Extensions.Primitives.StringValues typesValue))
        {
            if (string.IsNullOrEmpty(typesValue))
            {
                await WriteResultAsync(context, TypeNameIsEmpty(), BadRequest);
                return;
            }

            await WriteTypesAsync(context, schema, typesValue, indent);
        }
        else
        {
            await WriteSchemaAsync(context, schema, indent);
        }
    }

    private async Task WriteTypesAsync(
        HttpContext context,
        ISchema schema,
        string typeNames,
        bool indent)
    {
        var types = new List<INamedType>();

        foreach (string typeName in typeNames.Split(','))
        {
            if (!SchemaCoordinate.TryParse(typeName, out SchemaCoordinate? coordinate) ||
                coordinate.Value.MemberName is not null ||
                coordinate.Value.ArgumentName is not null)
            {
                await WriteResultAsync(context, InvalidTypeName(typeName), BadRequest);
                return;
            }

            if (!schema.TryGetType<INamedType>(coordinate.Value.Name, out INamedType? type))
            {
                await WriteResultAsync(context, TypeNotFound(typeName), NotFound);
                return;
            }

            types.Add(type);
        }

        context.Response.ContentType = ContentType.GraphQL;
        context.Response.Headers.SetContentDisposition(GetTypesFileName(types));
        await SerializeAsync(types, context.Response.Body, indent, context.RequestAborted);
        return;
    }

    private async Task WriteSchemaAsync(HttpContext context, ISchema schema, bool indent)
    {
        context.Response.ContentType = ContentType.GraphQL;
        context.Response.Headers.SetContentDisposition(GetSchemaFileName(schema));
        await SerializeAsync(schema, context.Response.Body, indent, context.RequestAborted);
    }

    private string GetTypesFileName(List<INamedType> types)
        => types.Count == 1
            ? $"{types[0].Name.Value}.graphql"
            : "types.graphql";

    private string GetSchemaFileName(ISchema schema)
        => schema.Name.IsEmpty || schema.Name.Equals(Schema.DefaultName)
            ? "schema.graphql"
            : schema.Name + ".schema.graphql";
}
