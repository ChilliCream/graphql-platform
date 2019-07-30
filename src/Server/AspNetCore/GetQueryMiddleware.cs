using System;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;
using System.Collections.Generic;

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
    public class GetQueryMiddleware
        : QueryMiddlewareBase
    {
        private const string _namedQueryIdentifier = "namedQuery";
        private const string _operationNameIdentifier = "operationName";
        private const string _queryIdentifier = "query";
        private const string _variablesIdentifier = "variables";

        private readonly IQueryExecutor _queryExecutor;

#if ASPNETCLASSIC
        public GetQueryMiddleware(
            RequestDelegate next,
            IQueryExecutor queryExecutor,
            IQueryResultSerializer resultSerializer,
            OwinContextAccessor owinContextAccessor,
            QueryMiddlewareOptions options)
            : base(next,
                resultSerializer,
                owinContextAccessor,
                options,
                queryExecutor.Schema.Services)
        {
            _queryExecutor = queryExecutor
                ?? throw new ArgumentNullException(nameof(queryExecutor));
        }
#else
        public GetQueryMiddleware(
            RequestDelegate next,
            IQueryExecutor queryExecutor,
            IQueryResultSerializer resultSerializer,        
            QueryMiddlewareOptions options)
                : base(next, resultSerializer, options)
        {
            _queryExecutor = queryExecutor
                ?? throw new ArgumentNullException(nameof(queryExecutor));
        }
#endif

        /// <inheritdoc />
        protected override bool CanHandleRequest(HttpContext context)
        {
            return string.Equals(
                context.Request.Method,
                HttpMethods.Get,
                StringComparison.Ordinal) &&
                    HasQueryParameter(context);
        }

        protected override async Task ExecuteRequestAsync(
            HttpContext context,
            IServiceProvider services)
        {
#if ASPNETCLASSIC
            IReadableStringCollection requestQuery = context.Request.Query;
#else
            IQueryCollection requestQuery = context.Request.Query;
#endif

            IQueryRequestBuilder builder =
                QueryRequestBuilder.New()
                    .SetQuery(requestQuery[_queryIdentifier])
                    .SetQueryName(requestQuery[_namedQueryIdentifier])
                    .SetOperation(requestQuery[_operationNameIdentifier]);

            string variables = requestQuery[_variablesIdentifier];
            if (variables != null
                && variables.Any()
                && Utf8GraphQLRequestParser.ParseJson(variables)
                    is IReadOnlyDictionary<string, object> v)
            {
                builder.SetVariableValues(v);
            }

            IReadOnlyQueryRequest request =
                await BuildRequestAsync(
                    context,
                    services,
                    builder)
                    .ConfigureAwait(false);

            IExecutionResult result = await _queryExecutor
                    .ExecuteAsync(request, context.GetCancellationToken())
                    .ConfigureAwait(false);

            await WriteResponseAsync(context.Response, result)
                .ConfigureAwait(false);
        }

        private static bool HasQueryParameter(HttpContext context)
        {
            return context.Request.QueryString.HasValue &&
                context.Request.Query.ToDictionary(i => i.Key, i => i.Value)
                    .ContainsKey(_queryIdentifier);
        }
    }
}
