using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Serialization;
using HotChocolate.Execution;
using HotChocolate.Language;
using Microsoft.AspNetCore.Http;
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

        public async override Task InvokeAsync(HttpContext context)
        {
            if (!HttpMethods.IsPost(context.Request.Method) ||
                !(context.GetGraphQLServerOptions()?.EnableMultipartRequests ?? false))
            {
                // if the request is not a post request we will just invoke the next
                // middleware and do nothing:
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

        protected override ValueTask<IReadOnlyList<GraphQLRequest>> GetRequestsFromBody(HttpRequest request, CancellationToken cancellationToken)
        {
            return RequestParser.ReadMultipartRequestAsync(request, cancellationToken);
        }
    }
}