using System.Text.Json;
using System.Text.Json.Serialization;
using HotChocolate.AspNetCore.Instrumentation;
using HotChocolate.AspNetCore.Parsers;
using HotChocolate.AspNetCore.Utilities;
using HotChocolate.Language;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;
using static System.Net.HttpStatusCode;
using static HotChocolate.AspNetCore.Utilities.ErrorHelper;
using static HotChocolate.AspNetCore.Properties.AspNetCorePipelineResources;
using HttpRequestDelegate = Microsoft.AspNetCore.Http.RequestDelegate;
using ThrowHelper = HotChocolate.AspNetCore.Utilities.ThrowHelper;

namespace HotChocolate.AspNetCore;

public sealed class HttpMultipartMiddleware : HttpPostMiddlewareBase
{
    private const string Operations = "operations";
    private const string Map = "map";
    private readonly FormOptions _formOptions;
    private readonly IOperationResult _multipartRequestError = MultiPartRequestPreflightRequired();

    public HttpMultipartMiddleware(
        HttpRequestDelegate next,
        HttpRequestExecutorProxy executor,
        IOptions<FormOptions> formOptions)
        : base(next, executor)
    {
        ArgumentNullException.ThrowIfNull(formOptions);
        _formOptions = formOptions.Value;
    }

    public override async Task InvokeAsync(HttpContext context)
    {
        if (HttpMethods.IsPost(context.Request.Method)
            && GetOptions(context).EnableMultipartRequests
            && context.ParseContentType() == RequestContentType.Form)
        {
            var session = await Executor.GetOrCreateSessionAsync(context.RequestAborted);

            if (!context.Request.Headers.ContainsKey(HttpHeaderKeys.Preflight)
                && GetOptions(context).EnforceMultipartRequestsPreflightHeader)
            {
                var headerResult = HeaderUtilities.GetAcceptHeader(context.Request);
                await session.WriteResultAsync(context, _multipartRequestError, headerResult.AcceptMediaTypes, BadRequest);
                return;
            }

            using (session.DiagnosticEvents.ExecuteHttpRequest(context, HttpRequestKind.HttpMultiPart))
            {
                await HandleRequestAsync(context, session, context.RequestAborted);
            }
        }
        else
        {
            // if the request is not a post multipart request or multipart requests are not enabled
            // we will just invoke the next middleware and do nothing:
            await NextAsync(context);
        }
    }

    protected override async ValueTask<IReadOnlyList<GraphQLRequest>> ParseRequestsFromBodyAsync(
        HttpContext context,
        ExecutorSession session)
    {
        IFormCollection? form;
        var httpRequest = context.Request;

        try
        {
            var formFeature = new FormFeature(httpRequest, _formOptions);
            form = await formFeature.ReadFormAsync(context.RequestAborted);
        }
        catch (Exception exception)
        {
            throw ThrowHelper.HttpMultipartMiddleware_Invalid_Form(exception);
        }

        // Parse the string values of interest from the IFormCollection
        var multipartRequest = ParseMultipartRequest(form);
        var requests = session.RequestParser.ParseRequest(
            multipartRequest.Operations);

        foreach (var graphQLRequest in requests)
        {
            InsertFilesIntoRequest(graphQLRequest, multipartRequest.FileMap);
        }

        return requests;
    }

    private static HttpMultipartRequest ParseMultipartRequest(IFormCollection form)
    {
        string? operations = null;
        Dictionary<string, string[]>? map = null;

        foreach (var field in form)
        {
            if (field.Key == Operations)
            {
                if (!field.Value.TryPeek(out operations) || string.IsNullOrEmpty(operations))
                {
                    throw ThrowHelper.HttpMultipartMiddleware_No_Operations_Specified();
                }
            }
            else if (field.Key == Map)
            {
                if (string.IsNullOrEmpty(operations))
                {
                    throw ThrowHelper.HttpMultipartMiddleware_Fields_Misordered();
                }

                field.Value.TryPeek(out var mapString);

                try
                {
                    map = JsonSerializer.Deserialize(mapString!, FormsMapJsonContext.Default.Map);
                }
                catch
                {
                    throw ThrowHelper.HttpMultipartMiddleware_InvalidMapJson();
                }
            }
        }

        if (operations is null)
        {
            throw ThrowHelper.HttpMultipartMiddleware_No_Operations_Specified();
        }

        if (map is null)
        {
            throw ThrowHelper.HttpMultipartMiddleware_MapNotSpecified();
        }

        // Validate file mappings and bring them in an easy to use format
        var pathToFileMap = MapFilesToObjectPaths(map, form.Files);

        return new HttpMultipartRequest(operations, pathToFileMap);
    }

    private static Dictionary<string, IFile> MapFilesToObjectPaths(
        IDictionary<string, string[]> map,
        IFormFileCollection files)
    {
        var pathToFileMap = new Dictionary<string, IFile>();

        foreach (var (filename, objectPaths) in map)
        {
            if (objectPaths is null || objectPaths.Length < 1)
            {
                throw ThrowHelper.HttpMultipartMiddleware_NoObjectPath(filename);
            }

            var file = filename.Length > 0 ? files.GetFile(filename) : null;

            if (file is null)
            {
                throw ThrowHelper.HttpMultipartMiddleware_FileMissing(filename);
            }

            foreach (var objectPath in objectPaths)
            {
                pathToFileMap.Add(objectPath, new UploadedFile(file));
            }
        }

        return pathToFileMap;
    }

    private static void InsertFilesIntoRequest(
        GraphQLRequest request,
        IDictionary<string, IFile> fileMap)
    {
        if (request.Variables is not [Dictionary<string, object?> mutableVariables])
        {
            throw new InvalidOperationException(
                HttpMultipartMiddleware_InsertFilesIntoRequest_VariablesImmutable);
        }

        foreach (var (objectPath, file) in fileMap)
        {
            var path = VariablePath.Parse(objectPath);

            if (!mutableVariables.TryGetValue(path.Key.Value, out var value))
            {
                throw ThrowHelper.HttpMultipartMiddleware_VariableNotFound(objectPath);
            }

            if (path.Key.Next is null)
            {
                mutableVariables[path.Key.Value] = new FileValueNode(file);
                continue;
            }

            if (value is null)
            {
                throw ThrowHelper.HttpMultipartMiddleware_VariableStructureInvalid();
            }

            mutableVariables[path.Key.Value] = RewriteVariable(
                objectPath,
                path.Key.Next,
                value,
                new FileValueNode(file));
        }
    }

    private static IValueNode RewriteVariable(
        string objectPath,
        IVariablePathSegment segment,
        object value,
        FileValueNode file)
    {
        if (segment is KeyPathSegment key && value is ObjectValueNode ov)
        {
            var pos = -1;

            for (var i = 0; i < ov.Fields.Count; i++)
            {
                if (ov.Fields[i].Name.Value.Equals(key.Value, StringComparison.Ordinal))
                {
                    pos = i;
                    break;
                }
            }

            if (pos == -1)
            {
                throw ThrowHelper.HttpMultipartMiddleware_VariableNotFound(objectPath);
            }

            var fields = ov.Fields.ToArray();
            var field = fields[pos];
            fields[pos] = field.WithValue(
                key.Next is not null
                    ? RewriteVariable(objectPath, key.Next, field.Value, file)
                    : file);
            return ov.WithFields(fields);
        }

        if (segment is IndexPathSegment index && value is ListValueNode lv)
        {
            var items = lv.Items.ToArray();
            var item = items[index.Value];
            items[index.Value] = index.Next is not null
                ? RewriteVariable(objectPath, index.Next, item, file)
                : file;
            return lv.WithItems(items);
        }

        throw ThrowHelper.HttpMultipartMiddleware_VariableNotFound(objectPath);
    }
}

[JsonSerializable(typeof(Dictionary<string, string[]>), TypeInfoPropertyName = "Map")]
internal partial class FormsMapJsonContext : JsonSerializerContext;
