using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Serialization;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using HttpRequestDelegate = Microsoft.AspNetCore.Http.RequestDelegate;
using static HotChocolate.AspNetCore.Properties.AspNetCoreResources;

namespace HotChocolate.AspNetCore
{
    public class HttpMultipartMiddleware : HttpPostMiddleware
    {
        private const string _operations = "operations";
        private const string _map = "map";

        public HttpMultipartMiddleware(
            HttpRequestDelegate next,
            IRequestExecutorResolver executorResolver,
            IHttpResultSerializer resultSerializer,
            IHttpRequestParser requestParser,
            NameString schemaName)
            : base(next, executorResolver, resultSerializer, requestParser, schemaName)
        {
        }

        public override async Task InvokeAsync(HttpContext context)
        {
            if (!HttpMethods.IsPost(context.Request.Method) ||
                !(context.GetGraphQLServerOptions()?.EnableMultipartRequests ?? true))
            {
                // if the request is not a post request or multipart requests are not enabled
                // we will just invoke the next middleware and do nothing:
                await NextAsync(context);
            }
            else
            {
                var contentType = ParseContentType(context.Request.ContentType);

                if (contentType == AllowedContentType.Form)
                {
                    await HandleRequestAsync(context, contentType);
                }
                else
                {
                    // the content type is unknown so we will invoke the next middleware.
                    await NextAsync(context);
                }
            }
        }

        protected override async ValueTask<IReadOnlyList<GraphQLRequest>> GetRequestsFromBody(
            HttpRequest httpRequest,
            CancellationToken cancellationToken)
        {
            IFormCollection? form;

            try
            {
                var formFeature = new FormFeature(httpRequest);
                form = await formFeature.ReadFormAsync(cancellationToken);
            }
            catch
            {
                throw ThrowHelper.HttpMultipartMiddleware_Form_Incomplete();
            }

            // Parse the string values of interest from the IFormCollection
            var parsedRequest = ParseMultipartRequest(form);
            var requests = RequestParser.ReadOperationsRequest(parsedRequest.Operations);

            foreach (var request in requests)
            {
                InsertFilesIntoRequest(request, parsedRequest.FileMap);
            }

            return requests;
        }

        private static HttpMultipartRequest ParseMultipartRequest(IFormCollection form)
        {
            string? operations = null;
            Dictionary<string, string[]>? map = null;

            foreach (var field in form)
            {
                if (field.Key == _operations)
                {
                    if (!field.Value.TryPeek(out operations) || string.IsNullOrEmpty(operations))
                    {
                        throw ThrowHelper.HttpMultipartMiddleware_No_Operations_Specified();
                    }
                }
                else if (field.Key == _map)
                {
                    if (string.IsNullOrEmpty(operations))
                    {
                        throw ThrowHelper.HttpMultipartMiddleware_Fields_Misordered();
                    }

                    field.Value.TryPeek(out var mapString);

                    try
                    {
                        map = JsonSerializer.Deserialize<Dictionary<string, string[]>>(mapString);
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

        private static IDictionary<string, IFile> MapFilesToObjectPaths(
            IDictionary<string, string[]> map,
            IFormFileCollection files)
        {
            var pathToFileMap = new Dictionary<string, IFile>();

            foreach ((var filename, var objectPaths) in map)
            {
                if (objectPaths is null || objectPaths.Length < 1)
                {
                    throw ThrowHelper.HttpMultipartMiddleware_NoObjectPath(filename);
                }

                IFormFile? file = filename is { Length: > 0 }
                    ? files.GetFile(filename)
                    : null;

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
            if (!(request.Variables is Dictionary<string, object?> mutableVariables))
            {
                throw new InvalidOperationException(
                    HttpMultipartMiddleware_InsertFilesIntoRequest_VariablesImmutable);
            }

            foreach ((string objectPath, IFile file) in fileMap)
            {
                var path = VariablePath.Parse(objectPath);

                if (!mutableVariables.TryGetValue(path.Key.Value, out object? value))
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
            if (segment is KeyPathSegment key &&
                value is ObjectValueNode ov)
            {
                var pos = -1;

                for (var i = 0; i < ov.Fields.Count; i++)
                {
                    if (ov.Fields[i].Name.Value.EqualsOrdinal(key.Value))
                    {
                        pos = i;
                        break;
                    }
                }

                if (pos == -1)
                {
                    throw ThrowHelper.HttpMultipartMiddleware_VariableNotFound(objectPath);
                }

                ObjectFieldNode[] fields = ov.Fields.ToArray();
                ObjectFieldNode field = fields[pos];
                fields[pos] = field.WithValue(
                    key.Next is not null
                        ? RewriteVariable(objectPath, key.Next, field.Value, file)
                        : file);
                return ov.WithFields(fields);
            }

            if (segment is IndexPathSegment index &&
                value is ListValueNode lv)
            {
                IValueNode[] items = lv.Items.ToArray();
                IValueNode item = items[index.Value];
                items[index.Value] = index.Next is not null
                    ? RewriteVariable(objectPath, index.Next, item, file)
                    : file;
                return lv.WithItems(items);
            }

            throw ThrowHelper.HttpMultipartMiddleware_VariableNotFound(objectPath);
        }
    }

    internal interface IVariablePathSegment
    {
        IVariablePathSegment? Next { get; }
    }

    internal class KeyPathSegment : IVariablePathSegment
    {
        public KeyPathSegment(string value, IVariablePathSegment? next)
        {
            Value = value;
            Next = next;
        }

        public string Value { get; }

        public IVariablePathSegment? Next { get; }
    }

    internal class IndexPathSegment : IVariablePathSegment
    {
        public IndexPathSegment(int value, IVariablePathSegment? next)
        {
            Value = value;
            Next = next;
        }

        public int Value { get; }

        public IVariablePathSegment? Next { get; }
    }

    internal class VariablePath
    {
        public VariablePath(KeyPathSegment key)
        {
            Key = key;
        }

        public KeyPathSegment Key { get; }

        public static VariablePath Parse(string s)
        {
            const string variables = nameof(variables);
            string[] segments = s.Split('.', StringSplitOptions.RemoveEmptyEntries);

            if (segments.Length < 2)
            {
                throw ThrowHelper.HttpMultipartMiddleware_InvalidPath(s);
            }

            if (!string.Equals(segments[0], variables, StringComparison.Ordinal))
            {
                throw ThrowHelper.HttpMultipartMiddleware_PathMustStartWithVariable();
            }

            IVariablePathSegment? segment = null;

            for (var i = segments.Length - 1; i >= 0; i--)
            {
                string item = segments[i];

                if (item.Equals(variables, StringComparison.Ordinal))
                {
                    continue;
                }

                segment = int.TryParse(item, out var index)
                    ? (IVariablePathSegment)new IndexPathSegment(index, segment)
                    : new KeyPathSegment(item, segment);
            }

            if (segment is KeyPathSegment key)
            {
                return new VariablePath(key);
            }

            throw new InvalidOperationException(VariablePath_Parse_FirstSegmentMustBeKey);
        }
    }
}
