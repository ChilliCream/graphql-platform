using System;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Execution;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

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

        public GetQueryMiddleware(
            RequestDelegate next,
            IQueryExecutor queryExecutor,
            IQueryResultSerializer resultSerializer,
            QueryMiddlewareOptions options)
                : base(next, queryExecutor, resultSerializer, options)
        { }

        /// <inheritdoc />
        protected override bool CanHandleRequest(HttpContext context)
        {
            return string.Equals(
                context.Request.Method,
                HttpMethods.Get,
                StringComparison.Ordinal) &&
                    HasQueryParameter(context);
        }

        /// <inheritdoc />
        protected override Task<IQueryRequestBuilder>
            CreateQueryRequestAsync(HttpContext context)
        {
            QueryRequestDto request = ReadRequest(context);
            return Task.FromResult(
                QueryRequestBuilder.New()
                    .SetQuery(request.Query)
                    .SetOperation(request.OperationName)
                    .SetVariableValues(QueryMiddlewareUtilities
                        .ToDictionary(request.Variables)));
        }

        private static QueryRequestDto ReadRequest(HttpContext context)
        {
#if ASPNETCLASSIC
            IReadableStringCollection requestQuery = context.Request.Query;
#else
            IQueryCollection requestQuery = context.Request.Query;
#endif
            string variables = requestQuery[_variablesIdentifier];

            return new QueryRequestDto
            {
                Query = requestQuery[_queryIdentifier],
                NamedQuery = requestQuery[_namedQueryIdentifier],
                OperationName = requestQuery[_operationNameIdentifier],
                Variables = (variables != null && variables.Length > 0)
                    ? JsonConvert.DeserializeObject<JObject>(
                        variables, QueryMiddlewareUtilities.JsonSettings)
                    : null
            };
        }

        private static bool HasQueryParameter(HttpContext context)
        {
            return context.Request.QueryString.HasValue &&
                context.Request.Query.ToDictionary(i => i.Key, i => i.Value)
                    .ContainsKey(_queryIdentifier);
        }
    }
}
