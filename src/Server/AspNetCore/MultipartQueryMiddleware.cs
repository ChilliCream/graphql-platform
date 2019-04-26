using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Types;
using HotChocolate.Utilities;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json;

#if ASPNETCLASSIC
using Microsoft.Owin;
using HttpContext = Microsoft.Owin.IOwinContext;
using RequestDelegate = Microsoft.Owin.OwinMiddleware;
#else
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
#endif

#if ASPNETCLASSIC
namespace HotChocolate.AspNetClassic
#else
namespace HotChocolate.AspNetCore
#endif
{
    public class MultipartQueryMiddleware
        : QueryMiddlewareBase
    {
        public MultipartQueryMiddleware(
            RequestDelegate next,
            IQueryExecutor queryExecutor,
            IQueryResultSerializer resultSerializer,
            QueryMiddlewareOptions options)
                : base(next, queryExecutor, resultSerializer, options)
        { }

        protected override bool CanHandleRequest(HttpContext context)
        {
            var contentType = (context.Request.ContentType ?? "")
                .Split(';')[0];
            return
                string.Equals(
                    context.Request.Method,
                    HttpMethods.Post,
                    StringComparison.Ordinal)
                && string.Equals(
                    contentType,
                    ContentType.Multipart,
                    StringComparison.Ordinal);
        }

        protected override async Task<IQueryRequestBuilder>
            CreateQueryRequestAsync(HttpContext context)
        {
#if ASPNETCLASSIC
            IServiceProvider serviceProvider = context.CreateRequestServices(
                Executor.Schema.Services);
#else
            IServiceProvider serviceProvider = context.CreateRequestServices();
#endif


            var boundary = MultipartRequestHelper.GetBoundary(context.Request.ContentType);
            var data = new List<(string name, string filename, Stream stream)>();
            var requestString = string.Empty;

#if ASPNETCLASSIC

            using (var content = new StreamContent(context.Request.Body))
            {

                content.Headers.ContentType =
                    System.Net.Http.Headers.MediaTypeHeaderValue.Parse(
                        context.Request.ContentType);

                var multipart = await content.ReadAsMultipartAsync();

                foreach (var item in multipart.Contents)
                {
                    var contentDispositionStr = item.Headers.GetValues("Content-Disposition")
                        .FirstOrDefault();
                    if (string.IsNullOrEmpty(contentDispositionStr))
                    {
                        continue;
                    }

                    var contentDisposition = new ContentDisposition(contentDispositionStr);

                    var name = contentDisposition.Parameters.ContainsKey("name")
                        ? contentDisposition.Parameters["name"]
                        : string.Empty;
                    var filename = contentDisposition.FileName;

                    if (name == "operations")
                    {
                        requestString = await item.ReadAsStringAsync();
                        continue;
                    }

                    data.Add((name, filename, await item.ReadAsStreamAsync()));
                }
            }

#else

            var reader = new MultipartReader(boundary, context.Request.Body);
            MultipartSection section;
            do
            {
                section = await reader.ReadNextSectionAsync();

                if (section == null || !section.Headers.ContainsKey("Content-Disposition")) continue;

                var contentDisposition =
                    new ContentDisposition(
                        section.Headers["Content-Disposition"].FirstOrDefault()
                        ?? string.Empty);

                var name = contentDisposition.Parameters.ContainsKey("name")
                    ? contentDisposition.Parameters["name"]
                    : string.Empty;
                var filename = contentDisposition.FileName;

                if (name == "operations")
                {
                    requestString = await section.ReadAsStringAsync();
                    continue;
                }

                data.Add((name, filename, section.Body));

            } while (section != null);

#endif

            var request = JsonConvert.DeserializeObject<QueryRequestDto>(requestString);

            if (request == null)
            {
                throw new Exception("Invalid request.");
            }

            var mapData = data.FirstOrDefault(x => x.name == "map");
            Dictionary<string, string[]> map = null;
            try
            {
                if (mapData != default)
                {
                    mapData.stream.Seek(0, SeekOrigin.Begin);
                    using (var sr = new StreamReader(mapData.stream))
                    {
                        map = JsonConvert.DeserializeObject<Dictionary<string, string[]>>(
                            await sr.ReadToEndAsync());
                    }
                }
            }
            catch (Exception)
            {
                throw new Exception(
                    "Can't read map value. " +
                    "See https://github.com/jaydenseric/graphql-multipart-request-spec#multipart-form-field-structure");
            }

            var variables = request.Variables.ToDictionary();

            if (map != null)
            {
                foreach (var mapItem in map)
                {
                    foreach (var variable in mapItem.Value)
                    {
                        var split = variable.Split('.');

                        var (_, filename, stream) = data.First(x => x.name == mapItem.Key);
                        stream.Seek(0, SeekOrigin.Begin);
                        var upload = new Upload(filename, stream);

                        var key = split[1];

                        if (split.Length == 2)
                        {
                            variables[key] = upload;
                        }
                        else
                        {
                            ICollection<Upload> collection;
                            if (variables[key] is ICollection<Upload> c)
                            {
                                collection = c;
                            }
                            else
                            {
                                collection = new Upload[] { };
                                variables[key] = collection;
                            }

                            collection.Add(upload);
                        }
                    }
                }
            }

            return QueryRequestBuilder.New()
                .SetQuery(request.Query)
                .SetOperation(request.OperationName)
                .SetVariableValues(variables)
                .SetServices(serviceProvider);
        }
    }
}
