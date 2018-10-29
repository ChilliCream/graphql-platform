using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json.Linq;

namespace HotChocolate.AspNetCore
{
    public class GetQueryMiddleware
        : QueryMiddlewareBase
    {
        private static readonly string _queryIdentifier = "query";
        private static readonly string _operationNameIdentifier = "operationName";
        private static readonly string _variablesIdentifier = "variables";
        private static readonly string _namedQueryIdentifier = "namedQuery";
        private static readonly string _getMethod = "Get";

        public GetQueryMiddleware(RequestDelegate next)
            : base(next)
        {
        }

        public GetQueryMiddleware(RequestDelegate next, string route)
            : base(next, route)
        {
        }

        protected override bool CanHandleRequest(HttpContext context)
        {
            return string.Equals(
                context.Request.Method, _getMethod,
                StringComparison.OrdinalIgnoreCase)
                && HasQueryParameter(context);
        }

        protected override Task<Execution.QueryRequest> CreateQueryRequest(
            HttpContext context)
        {
            QueryRequest request = ReadRequest(context);

            return Task.FromResult(
                new Execution.QueryRequest(request.Query, request.OperationName)
                {
                    VariableValues = QueryMiddlewareUtilities
                    .DeserializeVariables(request.Variables),
                    Services = QueryMiddlewareUtilities
                    .CreateRequestServices(context)
                });
        }

        private static QueryRequest ReadRequest(HttpContext context)
        {
            IQueryCollection requestQuery = context.Request.Query;
            StringValues variables = requestQuery[_variablesIdentifier];

            return new QueryRequest
            {
                Query = requestQuery[_queryIdentifier],
                NamedQuery = requestQuery[_namedQueryIdentifier],
                OperationName = requestQuery[_operationNameIdentifier],
                Variables = variables.Any() ? JObject.Parse(variables) : null
            };
        }

        private static bool HasQueryParameter(HttpContext context)
        {
            return context.Request.QueryString.HasValue &&
                   context.Request.Query.ContainsKey(_queryIdentifier);
        }
    }
}
