using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json.Linq;

namespace HotChocolate.AspNetCore
{
    internal static class GetRequest
    {
        private static readonly string _queryIdentifier = "query";
        private static readonly string _operationNameIdentifier = "operationName";
        private static readonly string _variablesIdentifier = "variables";
        private static readonly string _namedQueryIdentifier = "namedQuery";
        private static readonly string _getMethod = "Get";

        internal static bool IsGet(this HttpRequest request)
        {
            return request.Method.Equals(_getMethod, StringComparison.OrdinalIgnoreCase)
                   && HasQueryParameter(request.HttpContext);
        }

        internal static QueryRequest ReadRequest(HttpContext context)
        {
            IQueryCollection requestQuery = context.Request.Query;

            StringValues variables = requestQuery[_variablesIdentifier];
            return new QueryRequest
            {
                Query = requestQuery[_queryIdentifier].ToString(),
                NamedQuery = requestQuery[_namedQueryIdentifier].ToString(),
                OperationName = requestQuery[_operationNameIdentifier].ToString(),
                Variables = variables.Any()
                    ? JObject.Parse(variables.ToString().Trim('\"'))
                    : null
            };
        }



        private static bool HasQueryParameter(HttpContext context)
        {
            return context.Request.QueryString.HasValue &&
                   context.Request.Query.ContainsKey(_queryIdentifier);
        }
    }
}
