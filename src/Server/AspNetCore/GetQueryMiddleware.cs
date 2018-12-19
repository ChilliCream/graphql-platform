using System;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Execution;
using Newtonsoft.Json.Linq;

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
        private static readonly string _getMethod = "GET";
        private static readonly string _namedQueryIdentifier = "namedQuery";
        private static readonly string _operationNameIdentifier = "operationName";
        private static readonly string _queryIdentifier = "query";
        private static readonly string _variablesIdentifier = "variables";

        public GetQueryMiddleware(
            RequestDelegate next,
            IQueryExecuter queryExecuter,
            QueryMiddlewareOptions options)
                : base(next, queryExecuter, options)
        { }

        /// <inheritdoc />
        protected override bool CanHandleRequest(HttpContext context)
        {
            return string.Equals(context.Request.Method, _getMethod,
                StringComparison.Ordinal) && HasQueryParameter(context);
        }

        /// <inheritdoc />
        protected override Task<QueryRequest> CreateQueryRequest(
            HttpContext context)
        {
            QueryRequestDto request = ReadRequest(context);
#if ASPNETCLASSIC
            IServiceProvider serviceProvider = context.CreateRequestServices(
                Services);
#else

            IServiceProvider serviceProvider = context.CreateRequestServices();
#endif

            return Task.FromResult(
                new QueryRequest(request.Query, request.OperationName)
                {
                    VariableValues = QueryMiddlewareUtilities
                        .ToDictionary(request.Variables),
                    Services = serviceProvider
                });
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
                Variables = (variables != null && variables.Any())
                    ? JObject.Parse(variables)
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
