using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Serialization;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Primitives;
using HttpRequestDelegate = Microsoft.AspNetCore.Http.RequestDelegate;

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
                AllowedContentType contentType = ParseContentType(context.Request.ContentType);
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
                // TODO : throw helper
                throw new GraphQLRequestException(
                    ErrorBuilder.New()
                        .SetMessage("At least an 'operations' and a 'map' field need to be present.")
                        .SetCode("// TODO CODE HC")
                        .Build());
            }

            // Parse the string values of interest from the IFormCollection
            HttpMultipartRequest parsedRequest = ParseMultipartRequest(form);

            IReadOnlyList<GraphQLRequest> requests = RequestParser.ReadOperationsRequest(parsedRequest.Operations);

            foreach (GraphQLRequest request in requests)
            {
                InsertFilesIntoRequest(request, parsedRequest.FileMap);
            }

            return requests;
        }

        private static HttpMultipartRequest ParseMultipartRequest(
            IFormCollection form)
        {
            string? operations = null;
            Dictionary<string, string[]>? map = null;

            foreach (KeyValuePair<string, StringValues> field in form)
            {
                if (field.Key == _operations)
                {
                    if (!field.Value.TryPeek(out operations) || string.IsNullOrEmpty(operations))
                    {
                        // TODO : throw helper
                        throw new GraphQLRequestException(
                            ErrorBuilder.New()
                                .SetMessage("No '{0}' specified.", _operations)
                                .SetCode("// TODO CODE HC")
                                .Build());
                    }
                }
                else if (field.Key == _map)
                {
                    if (string.IsNullOrEmpty(operations))
                    {
                        // TODO : throw helper
                        throw new GraphQLRequestException(
                            ErrorBuilder.New()
                                .SetMessage("Misordered multipart fields; '{0}' should follow ‘{1}’.", _map, _operations)
                                .SetCode("// TODO CODE HC")
                                .Build());
                    }

                    field.Value.TryPeek(out var mapString);

                    try
                    {
                        map = JsonSerializer.Deserialize<Dictionary<string, string[]>>(mapString);
                    }
                    catch
                    {
                        // TODO : throw helper
                        throw new GraphQLRequestException(
                            ErrorBuilder.New()
                                .SetMessage(
                                    "Invalid JSON in the ‘{0}’ multipart field; Expected type of Dictionary<string, string[]>.", _map)
                                .SetCode("// TODO CODE HC")
                                .Build());
                    }
                }
            }

            if (map is null)
            {
                // TODO : throw helper
                throw new GraphQLRequestException(
                    ErrorBuilder.New()
                        .SetMessage("No '{0}' specified.", _map)
                        .SetCode("// TODO CODE HC")
                        .Build());
            }

            // Validate file mappings and bring them in an easy to use format
            IDictionary<string, IFormFile> pathToFileMap =
                MapFilesToObjectPaths(map, form.Files);

            return new HttpMultipartRequest(operations, pathToFileMap);
        }

        private static IDictionary<string, IFormFile> MapFilesToObjectPaths(
            IDictionary<string, string[]> map,
            IFormFileCollection files)
        {
            var pathToFileMap = new Dictionary<string, IFormFile>();

            foreach ((var filename, var objectPaths) in map)
            {
                if (string.IsNullOrEmpty(filename))
                {
                    // TODO : throw helper
                    throw new GraphQLRequestException(
                        ErrorBuilder.New()
                            .SetMessage("Entry with missing key in '{0}'.", _map)
                            .SetCode("// TODO CODE HC")
                            .Build());
                }

                if (objectPaths is null || objectPaths.Length < 1)
                {
                    // TODO : throw helper
                    throw new GraphQLRequestException(
                        ErrorBuilder.New()
                            .SetMessage("No object paths specified for key '{0}' in '{1}'.", filename, _map)
                            .SetCode("// TODO CODE HC")
                            .Build());
                }

                IFormFile? file = files.GetFile(filename);

                if (file is null)
                {
                    // TODO : throw helper
                    throw new GraphQLRequestException(
                        ErrorBuilder.New()
                            .SetMessage("File of key '{0}' is missing.", filename)
                            .SetCode("// TODO CODE HC")
                            .Build());
                }

                foreach (var objectPath in objectPaths)
                {
                    pathToFileMap.Add(objectPath, file);
                }
            }

            return pathToFileMap;
        }

        // TODO : This is not covered by tests yet
        private static void InsertFilesIntoRequest(
            GraphQLRequest request,
            IDictionary<string, IFormFile> fileMap)
        {
            if (!(request.Variables is Dictionary<string, object?> mutableVariables))
            {
                return;
            }

            foreach ((string objectPath, IFormFile file) in fileMap)
            {
                var pathParts = objectPath.Split('.', System.StringSplitOptions.RemoveEmptyEntries);

                if (pathParts.Length < 2)
                {
                    // TODO : throw helper
                    throw new GraphQLRequestException(
                        ErrorBuilder.New()
                            .SetMessage("Invalid object path in '{0}'.", _map)
                            .SetCode("// TODO CODE HC")
                            .Build());
                }

                if (pathParts[0] != "variables")
                {
                    // TODO : This is (hopefully) just a limitation for now.
                    throw new System.NotSupportedException("Files can currently only be inserted into 'variables'.");
                }

                var variableName = pathParts[1];

                if (!mutableVariables.ContainsKey(variableName))
                {
                    // TODO : throw helper
                    throw new GraphQLRequestException(
                        ErrorBuilder.New()
                            .SetMessage("No variable with the name '{0}' was specified in the original request.", variableName)
                            .SetCode("// TODO CODE HC")
                            .Build());
                }

                switch (pathParts.Length)
                {
                    case 2:
                        // single file upload, e.g. 'variables.file'
                        mutableVariables[variableName] = new FileValueNode(file);
                        break;
                    case 3:
                        // multi file upload, e.g. 'variables.files.1'
                        if (!int.TryParse(pathParts[2], out var fileIndex))
                        {
                            continue;
                        }

                        List<IValueNode> list;

                        if (mutableVariables[variableName] is ListValueNode listValueNode &&
                            listValueNode.Items is List<IValueNode> listValue)
                        {
                            list = listValue;
                        }
                        else
                        {
                            list = new List<IValueNode>();
                        }

                        // we don't know the size of the file list beforehand so we have to resize dynamically
                        for (var i = list.Count; i <= fileIndex; i++)
                        {
                            list.Add(NullValueNode.Default);
                        }

                        list[fileIndex] = new FileValueNode(file);

                        // TODO : We create a new ListValueNode for every new file that is inserted
                        mutableVariables[variableName] = new ListValueNode(list);
                        break;
                    default:
                        // TODO : This is (hopefully) just a limitation for now.
                        throw new System.NotSupportedException("Files can currently only be inserted into top-level 'variables'. Input objects are not yet supported.");
                }
            }
        }
    }
}
