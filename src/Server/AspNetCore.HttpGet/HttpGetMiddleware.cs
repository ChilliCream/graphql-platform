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
    public class HttpGetMiddleware
        : QueryMiddlewareBase
    {
        private const string _namedQueryIdentifier = "namedQuery";
        private const string _operationNameIdentifier = "operationName";
        private const string _queryIdentifier = "query";
        private const string _variablesIdentifier = "variables";

        private readonly IQueryExecutor _queryExecutor;
        private readonly IQueryResultSerializer _resultSerializer;

#if ASPNETCLASSIC
        public HttpGetMiddleware(
            RequestDelegate next,
            IHttpGetMiddlewareOptions options,
            OwinContextAccessor owinContextAccessor,
            IQueryExecutor queryExecutor,
            IQueryResultSerializer resultSerializer)
            : base(next,
                options,
                owinContextAccessor,
                queryExecutor.Schema.Services)
        {
            _queryExecutor = queryExecutor
                ?? throw new ArgumentNullException(nameof(queryExecutor));
            _resultSerializer = resultSerializer
                ?? throw new ArgumentNullException(nameof(resultSerializer));
        }
#else
        public HttpGetMiddleware(
            RequestDelegate next,
            IHttpGetMiddlewareOptions options,
            IQueryExecutor queryExecutor,
            IQueryResultSerializer resultSerializer)
                : base(next, options)
        {
            _queryExecutor = queryExecutor
                ?? throw new ArgumentNullException(nameof(queryExecutor));
            _resultSerializer = resultSerializer
                ?? throw new ArgumentNullException(nameof(resultSerializer));
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
            var builder = QueryRequestBuilder.New();

#if ASPNETCLASSIC
            IReadableStringCollection requestQuery = context.Request.Query;
#else
            IQueryCollection requestQuery = context.Request.Query;
#endif

            builder
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

            SetResponseHeaders(
                context.Response,
                _resultSerializer.ContentType);

            await _resultSerializer.SerializeAsync(
                result,
                context.Response.Body,
                context.GetCancellationToken())
                .ConfigureAwait(false);
        }

        private static bool HasQueryParameter(HttpContext context)
        {
#if ASPNETCLASSIC
            return context.Request.Query[_queryIdentifier] != null
                || context.Request.Query[_namedQueryIdentifier] != null;
#else
            return context.Request.Query[_queryIdentifier].Count != 0
                || context.Request.Query[_namedQueryIdentifier].Count != 0;
#endif
        }
    }
}
