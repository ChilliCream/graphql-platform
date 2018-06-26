using System;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.AspNetCore
{
    internal static class GetRequest
    {
        private const string _queryIdentifier = "query";
        private const string _getMethod = "Get";

        internal static bool IsGet(this HttpRequest request)
        {
            return request.Method.Equals(_getMethod, StringComparison.OrdinalIgnoreCase)
                   && HasQueryParameter(request.HttpContext);
        }

        internal static QueryRequest ReadRequest(HttpContext context)
        {
            return new QueryRequest()
            {
                Query = context.Request.Query[_queryIdentifier].ToString()
            };
        }

        private static bool HasQueryParameter(HttpContext context)
        {
            return context.Request.QueryString.HasValue &&
                   context.Request.Query.ContainsKey(_queryIdentifier);
        }
    }
}
