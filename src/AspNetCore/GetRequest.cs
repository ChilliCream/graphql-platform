using System;
using Microsoft.AspNetCore.Http;
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
            return new QueryRequest
            {
                NamedQuery = context.Request.Query[_namedQueryIdentifier].ToString(),
                OperationName = context.Request.Query[_operationNameIdentifier].ToString(),
                Query = context.Request.Query[_queryIdentifier].ToString(),
                Variables = JObject.Parse(context.Request.Query[_variablesIdentifier].ToString()),
            };
        }

        private static bool HasQueryParameter(HttpContext context)
        {
            return context.Request.QueryString.HasValue &&
                   context.Request.Query.ContainsKey(_queryIdentifier);
        }
    }
}
