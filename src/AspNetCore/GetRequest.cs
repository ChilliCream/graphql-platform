using System;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.AspNetCore
{
    internal static class GetRequest
    {
        private const string _queryIdentifier = "query";
        private const string _method = "Get";

        internal static bool IsGet(this HttpRequest request)
        {
            return request.Method.Equals(_method, StringComparison.OrdinalIgnoreCase);
        }

        internal static QueryRequest ReadRequest(HttpContext context)
        {
            string query = string.Empty;

            if (context.Request.QueryString.HasValue &&
                context.Request.Query.ContainsKey(_queryIdentifier))
            {
                query = context.Request.Query[_queryIdentifier].ToString();
            }

            return new QueryRequest() {Query = query};
        }
    }
}
