using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Serialization;
using HotChocolate.Execution;
using HotChocolate.Language;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using HttpRequestDelegate = Microsoft.AspNetCore.Http.RequestDelegate;

namespace HotChocolate.AspNetCore
{
    public class HttpMultipartMiddleware : HttpPostMiddleware
    {
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
                !(context.GetGraphQLServerOptions()?.EnableMultipartRequests ?? false))
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
            var formFeature = new FormFeature(httpRequest);
            IFormCollection? form = await formFeature.ReadFormAsync(cancellationToken);

            (IReadOnlyList<GraphQLRequest> Requests, IDictionary<string, string[]> Map) result =
                RequestParser.ReadFormRequest(form);

            if (form.Files.Count > 0)
            {
                foreach (GraphQLRequest request in result.Requests)
                {
                    InsertFilesIntoRequest(request, result.Map, form.Files);
                }
            }

            return result.Requests;
        }

        private static void InsertFilesIntoRequest(GraphQLRequest request,
            IDictionary<string, string[]> map,
            IFormFileCollection files)
        {
            if (!(request.Variables is Dictionary<string, object?> mutableVariables))
            {
                return;
            }

            foreach (KeyValuePair<string, string[]> mapPair in map)
            {
                var filename = mapPair.Key;
                var objectPaths = mapPair.Value;

                if (string.IsNullOrEmpty(filename))
                {
                    // TODO : how to handle
                    continue;
                }

                if (objectPaths is null || objectPaths.Length < 1)
                {
                    // TODO : how to handle
                    continue;
                }

                IFormFile? file = files.GetFile(filename);

                if (file is null)
                {
                    // TODO : how to handle
                    continue;
                }

                foreach (var objectPath in objectPaths)
                {
                    var parts = objectPath.Split('.');

                    if (parts.Length < 2)
                    {
                        // TODO : how to handle
                        continue;
                    }


                    if (parts[0] != "variables")
                    {
                        // nested properties are currently not supported
                        continue;
                    }

                    var variableName = parts[1];

                    switch (parts.Length)
                    {
                        case 2:
                            // single file upload, e.g. 'variables.file'
                            mutableVariables[variableName] = file;
                            break;
                        case 3:
                            // multi file upload, e.g. 'variables.files.1'
                            if (!int.TryParse(parts[2], out var fileIndex))
                            {
                                continue;
                            }

                            List<IFormFile?> list;

                            if (mutableVariables[variableName] is List<IFormFile?> variableList)
                            {
                                list = variableList;
                            }
                            else
                            {
                                list = new List<IFormFile?>();

                                mutableVariables[variableName] = list;
                            }

                            // we don't know the size of the file list beforehand so we have to resize dynamically
                            for (var i = list.Count; i <= fileIndex; i++)
                            {
                                list.Add(null);
                            }

                            list[fileIndex] = file;

                            break;
                        default:
                            // nested object, which is currently not supported
                            continue;
                    }
                }
            }
        }
    }
}
