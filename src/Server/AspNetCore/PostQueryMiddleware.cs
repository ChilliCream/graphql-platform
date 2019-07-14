using System.Collections.Generic;
using System;
using System.IO;
using System.Threading.Tasks;
using HotChocolate.Execution;
using System.Buffers;
using HotChocolate.Language;

#if ASPNETCLASSIC
using Microsoft.Owin;
using HttpContext = Microsoft.Owin.IOwinContext;
using RequestDelegate = Microsoft.Owin.OwinMiddleware;
#else
using Microsoft.AspNetCore.Http;
#endif

#if ASPNETCLASSIC
namespace HotChocolate.AspNetClassic
#else
namespace HotChocolate.AspNetCore
#endif
{
    public class PostQueryMiddleware
        : QueryMiddlewareBase
    {
        private readonly RequestHelper _requestHelper;

        public PostQueryMiddleware(
            RequestDelegate next,
            IQueryExecutor queryExecutor,
            IQueryResultSerializer resultSerializer,
            IDocumentCache documentCache,
            IDocumentHashProvider documentHashProvider,
            QueryMiddlewareOptions options)
                : base(next, queryExecutor, resultSerializer, options)
        {
            _requestHelper = new RequestHelper(
                documentCache,
                documentHashProvider,
                options.MaxRequestSize,
                options.ParserOptions);
        }

        protected override bool CanHandleRequest(HttpContext context)
        {
            return string.Equals(
                context.Request.Method,
                HttpMethods.Post,
                StringComparison.Ordinal);
        }

        protected override async Task<IQueryRequestBuilder>
            CreateQueryRequestAsync(HttpContext context)
        {
            using (Stream stream = context.Request.Body)
            {
                IReadOnlyList<GraphQLRequest> batch = null;

                switch (context.Request.ContentType.Split(';')[0])
                {
                    case ContentType.Json:
                        batch = await _requestHelper
                            .ReadJsonRequestAsync(stream)
                            .ConfigureAwait(false);
                        break;

                    case ContentType.GraphQL:
                        batch = await _requestHelper
                            .ReadGraphQLQueryAsync(stream)
                            .ConfigureAwait(false);
                        break;

                    default:
                        throw new NotSupportedException();
                }


                // TODO : batching support has to be added later
                GraphQLRequest request = batch[0];

                return QueryRequestBuilder.New()
                    .SetQuery(request.Query)
                    .SetQueryName(request.QueryName)
                    .SetQueryName(request.QueryName) // TODO : we should have a hash here
                    .SetOperation(request.OperationName)
                    .SetVariableValues(request.Variables)
                    .SetProperties(request.Extensions);
            }
        }
    }
}
