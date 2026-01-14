using System.Text;
using HotChocolate.AspNetCore.Instrumentation;
using Microsoft.AspNetCore.Http;
using static System.Net.HttpStatusCode;
using static HotChocolate.AspNetCore.Utilities.ErrorHelper;
using HttpRequestDelegate = Microsoft.AspNetCore.Http.RequestDelegate;

namespace HotChocolate.AspNetCore;

public sealed class HttpGetSchemaMiddleware : MiddlewareBase
{
    private static readonly AcceptMediaType[] s_mediaTypes =
    [
        new AcceptMediaType(
            ContentType.Types.Application,
            ContentType.SubTypes.GraphQLResponse,
            null,
            default)
    ];

    private readonly MiddlewareRoutingType _routing;
    private readonly PathString _path;

    public HttpGetSchemaMiddleware(
        HttpRequestDelegate next,
        HttpRequestExecutorProxy executor,
        PathString path,
        MiddlewareRoutingType routing)
        : base(next, executor)
    {
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
            var session = await Executor.GetOrCreateSessionAsync(context.RequestAborted);
            var options = GetOptions(context);

            using (session.DiagnosticEvents.ExecuteHttpRequest(context, HttpRequestKind.HttpGetSchema))
            {
                await HandleRequestAsync(context, session, options);
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
        if (request.Path.StartsWithSegments(_path, StringComparison.OrdinalIgnoreCase, out var remaining))
        {
            return remaining.Equals("/schema", StringComparison.OrdinalIgnoreCase)
                || remaining.Equals("/schema/", StringComparison.OrdinalIgnoreCase)
                || remaining.Equals("/schema.graphql", StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    private static async Task HandleRequestAsync(HttpContext context, ExecutorSession session, GraphQLServerOptions options)
    {
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
                await session.WriteResultAsync(
                    context,
                    TypeNameIsEmpty(),
                    s_mediaTypes,
                    BadRequest);
                return;
            }

            await WriteTypesAsync(context, session, s, true);
        }
        else
        {
            await session.WriteSchemaAsync(context);
        }
    }

    private static async Task WriteTypesAsync(
        HttpContext context,
        ExecutorSession session,
        string typeNames,
        bool indent)
    {
        var types = new List<ITypeDefinition>();

        foreach (var typeName in typeNames.Split(','))
        {
            if (!SchemaCoordinate.TryParse(typeName, out var coordinate)
                || coordinate.Value.MemberName is not null
                || coordinate.Value.ArgumentName is not null)
            {
                await session.WriteResultAsync(
                    context,
                    InvalidTypeName(typeName),
                    s_mediaTypes,
                    BadRequest);
                return;
            }

            if (!session.Schema.Types.TryGetType<ITypeDefinition>(coordinate.Value.Name, out var type))
            {
                await session.WriteResultAsync(
                    context,
                    TypeNotFound(typeName),
                    s_mediaTypes,
                    NotFound);
                return;
            }

            types.Add(type);
        }

        context.Response.ContentType = ContentType.GraphQL;
        context.Response.Headers.SetContentDisposition(GetTypesFileName(types));
        await PrintTypesAsync(types, context.Response.Body, indent, context.RequestAborted);
    }

    private static string GetTypesFileName(List<ITypeDefinition> types)
        => types.Count == 1
            ? $"{types[0].Name}.graphql"
            : "types.graphql";

    private static async Task PrintTypesAsync(
        List<ITypeDefinition> types,
        Stream stream,
        bool indent,
        CancellationToken cancellationToken)
    {
        var next = false;
        await using var streamWriter = new StreamWriter(stream, Encoding.UTF8, 1024, leaveOpen: true);

        foreach (var type in types)
        {
            if (next)
            {
                await streamWriter.WriteLineAsync();
            }

            var s = type.ToSyntaxNode().ToString(indent);
            await streamWriter.WriteLineAsync(s.AsMemory(), cancellationToken);
            next = true;
        }
    }
}
