using HotChocolate.AspNetCore.Instrumentation;
using HotChocolate.AspNetCore.Serialization;
using HotChocolate.Execution.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
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
    private readonly PathString _path;

    public HttpGetSchemaMiddleware(
        HttpRequestDelegate next,
        IRequestExecutorResolver executorResolver,
        IHttpResponseFormatter responseFormatter,
        IServerDiagnosticEvents diagnosticEvents,
        string schemaName,
        PathString path,
        MiddlewareRoutingType routing)
        : base(next, executorResolver, responseFormatter, schemaName)
    {
        _diagnosticEvents = diagnosticEvents ?? throw new ArgumentNullException(nameof(diagnosticEvents));
        _path = path;
        _routing = routing;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var handle = _routing == MiddlewareRoutingType.Integrated
            ? HttpMethods.IsGet(context.Request.Method)
                && (context.Request.Query.ContainsKey("SDL") || IsSchemaPath(context.Request))
                && GetOptions(context).EnableSchemaRequests
            : HttpMethods.IsGet(context.Request.Method)
                && GetOptions(context).EnableSchemaRequests;

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

    private bool IsSchemaPath(HttpRequest request)
    {
        if(request.Path.StartsWithSegments(_path, StringComparison.OrdinalIgnoreCase, out var remaining))
        {
            return remaining.Equals("/schema", StringComparison.OrdinalIgnoreCase)
                || remaining.Equals("/schema/", StringComparison.OrdinalIgnoreCase)
                || remaining.Equals("/schema.graphql", StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    private async Task HandleRequestAsync(HttpContext context)
    {
        var executor = await GetExecutorAsync(context.RequestAborted);
        context.Items[WellKnownContextData.Schema] = executor.Schema;
        var options = executor.Schema.Services.GetRequiredService<IRequestExecutorOptionsAccessor>();

        if (!options.EnableSchemaFileSupport)
        {
            context.Response.StatusCode = 404;
            return;
        }

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

            await WriteTypesAsync(context, executor.Schema, s, true);
        }
        else
        {
            await WriteSchemaAsync(context, executor);
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
            if (!SchemaCoordinate.TryParse(typeName, out var coordinate)
                || coordinate.Value.MemberName is not null
                || coordinate.Value.ArgumentName is not null)
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

    private ValueTask WriteSchemaAsync(
        HttpContext context,
        IRequestExecutor executor)
        => ResponseFormatter.FormatAsync(
            context.Response,
            executor.Schema,
            executor.Version,
            context.RequestAborted);

    private string GetTypesFileName(List<INamedType> types)
        => types.Count == 1
            ? $"{types[0].Name}.graphql"
            : "types.graphql";
}
