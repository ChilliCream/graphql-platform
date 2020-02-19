using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using HotChocolate.Execution;
using HotChocolate.Language;

namespace HotChocolate.AspNetCore
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

        public HttpGetMiddleware(
            RequestDelegate next,
            IQueryExecutor queryExecutor,
            IQueryResultSerializer resultSerializer,
            IErrorHandler errorHandler)
            : base(next, resultSerializer, errorHandler)
        {
            _queryExecutor = queryExecutor
                ?? throw new ArgumentNullException(nameof(queryExecutor));
            _resultSerializer = resultSerializer
                ?? throw new ArgumentNullException(nameof(resultSerializer));
        }

        /// <inheritdoc />
        protected override bool CanHandleRequest(HttpContext context) =>
            string.Equals(context.Request.Method, HttpMethods.Get, StringComparison.Ordinal)
                && HasQueryParameter(context);

        protected override async Task ExecuteRequestAsync(
            HttpContext context,
            IServiceProvider services)
        {
            var builder = QueryRequestBuilder.New();
            IQueryCollection requestQuery = context.Request.Query;

            builder
                .SetQuery(requestQuery[_queryIdentifier])
                .SetQueryName(requestQuery[_namedQueryIdentifier])
                .SetOperation(requestQuery[_operationNameIdentifier]);

            string variables = requestQuery[_variablesIdentifier];
            if (variables != null
                && variables.Length > 0
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

            SetResponseHeaders(context.Response, _resultSerializer.ContentType);

            await _resultSerializer.SerializeAsync(
                result,
                context.Response.Body,
                context.GetCancellationToken())
                .ConfigureAwait(false);
        }

        private static bool HasQueryParameter(HttpContext context)
        {
            return context.Request.Query[_queryIdentifier].Count != 0
                || context.Request.Query[_namedQueryIdentifier].Count != 0;
        }
    }
}
