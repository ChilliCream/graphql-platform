using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Utilities;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json;

#if ASPNETCLASSIC
using Microsoft.Owin;
using HttpContext = Microsoft.Owin.IOwinContext;
using RequestDelegate = Microsoft.Owin.OwinMiddleware;
#else
using Microsoft.AspNetCore.Http;
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

            var content = new MultipartFormDataContent(boundary);
            foreach (var item in content)
            {
                var name = item.Headers.GetValues("name").SingleOrDefault();
                if (string.IsNullOrEmpty(name))
                {
                    continue;
                }

                var filename = item.Headers.GetValues("filename").SingleOrDefault();

                if (name == "operations")
                {
                    requestString = await item.ReadAsStringAsync();
                    continue;
                }

                data.Add((name, filename, await item.ReadAsStreamAsync()));
            }

#else

            var reader = new MultipartReader(boundary, context.Request.Body);
            MultipartSection section;
            do
            {
                section = await reader.ReadNextSectionAsync();

                if (section == null || !section.Headers.ContainsKey("name")) continue;

                var name = section.Headers["name"];
                var filename = section.Headers.ContainsKey("filename")
                    ? section.Headers["filename"].FirstOrDefault()
                    : string.Empty;

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

            var map = data.FirstOrDefault(x => x.name == "map");

            return QueryRequestBuilder.New()
                .SetQuery(request.Query)
                .SetOperation(request.OperationName)
                .SetVariableValues(
                    QueryMiddlewareUtilities.ToDictionary(request.Variables))
                .SetServices(serviceProvider);
        }
    }
}
