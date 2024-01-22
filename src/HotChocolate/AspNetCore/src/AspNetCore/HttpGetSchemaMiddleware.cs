using HotChocolate.AspNetCore.Instrumentation;
using HotChocolate.AspNetCore.Serialization;
using HotChocolate.Utilities;
using Microsoft.AspNetCore.Http;
using static System.Net.HttpStatusCode;
using static HotChocolate.AspNetCore.ErrorHelper;
using static HotChocolate.SchemaPrinter;
using HttpRequestDelegate = Microsoft.AspNetCore.Http.RequestDelegate;

namespace HotChocolate.AspNetCore;

public sealed class HttpGetSchemaMiddleware : MiddlewareBase
{
    private static readonly AcceptMediaType[] _mediaTypes =
    [
        new AcceptMediaType(
            ContentType.Types.Application,
            ContentType.SubTypes.GraphQLResponse,
            null,
            default),
    ];
    private readonly MiddlewareRoutingType _routing;
    private readonly IServerDiagnosticEvents _diagnosticEvents;

    public HttpGetSchemaMiddleware(
        HttpRequestDelegate next,
        IRequestExecutorResolver executorResolver,
        IHttpResponseFormatter responseFormatter,
        IServerDiagnosticEvents diagnosticEvents,
        string schemaName,
        MiddlewareRoutingType routing)
        : base(next, executorResolver, responseFormatter, schemaName)
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
                GetOptions(context).EnableSchemaRequests
            : HttpMethods.IsGet(context.Request.Method) &&
                GetOptions(context).EnableSchemaRequests;

        if (handle)
        {
            if (!IsDefaultSchema)
            {
                context.Items[WellKnownContextData.SchemaName] = SchemaName;
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
        var schema = await GetSchemaAsync(context.RequestAborted);
        context.Items[WellKnownContextData.Schema] = schema;

        var indent =
            !(context.Request.Query.ContainsKey("indentation") &&
                string.Equals(
                    context.Request.Query["indentation"].FirstOrDefault(),
                    "none",
                    StringComparison.OrdinalIgnoreCase));

        if (context.Request.Query.TryGetValue("types", out var typesValue))
        {
            string? s = typesValue;

            if (string.IsNullOrEmpty(s))
            {
                await WriteResultAsync(
                    context,
                    TypeNameIsEmpty(),
                    _mediaTypes,
                    BadRequest);
                return;
            }

            await WriteTypesAsync(context, schema, s, indent);
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

        foreach (var typeName in typeNames.Split(','))
        {
            if (!SchemaCoordinate.TryParse(typeName, out var coordinate) ||
                coordinate.Value.MemberName is not null ||
                coordinate.Value.ArgumentName is not null)
            {
                await WriteResultAsync(
                    context,
                    InvalidTypeName(typeName),
                    _mediaTypes,
                    BadRequest);
                return;
            }

            if (!schema.TryGetType<INamedType>(coordinate.Value.Name, out var type))
            {
                await WriteResultAsync(
                    context,
                    TypeNotFound(typeName),
                    _mediaTypes,
                    NotFound);
                return;
            }

            types.Add(type);
        }

        context.Response.ContentType = ContentType.GraphQL;
        context.Response.Headers.SetContentDisposition(GetTypesFileName(types));
        await PrintAsync(types, context.Response.Body, indent, context.RequestAborted);
    }

    private async Task WriteSchemaAsync(HttpContext context, ISchema schema, bool indent)
    {
        context.Response.ContentType = ContentType.GraphQL;
        context.Response.Headers.SetContentDisposition(GetSchemaFileName(schema));
        await PrintAsync(schema, context.Response.Body, indent, context.RequestAborted);
    }

    private string GetTypesFileName(List<INamedType> types)
        => types.Count == 1
            ? $"{types[0].Name}.graphql"
            : "types.graphql";

    private string GetSchemaFileName(ISchema schema)
        => schema.Name is null || schema.Name.EqualsOrdinal(Schema.DefaultName)
            ? "schema.graphql"
            : schema.Name + ".schema.graphql";
}
