using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using HotChocolate.Execution;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace HotChocolate.AspNetCore
{
    public class PostQueryMiddleware
        : QueryMiddlewareBase
    {
        private const string _postMethod = "POST";

        public PostQueryMiddleware(
            RequestDelegate next,
            QueryExecuter queryExecuter)
            : base(next, queryExecuter)
        {
        }

        protected override bool CanHandleRequest(HttpContext context)
        {
            return string.Equals(
                context.Request.Method, _postMethod,
                StringComparison.Ordinal);
        }

        protected override async Task<Execution.QueryRequest> CreateQueryRequest(
            HttpContext context)
        {
            QueryRequest request = await ReadRequestAsync(context);

            return new Execution.QueryRequest(
                request.Query, request.OperationName)
            {
                VariableValues = QueryMiddlewareUtilities
                    .DeserializeVariables(request.Variables),
                Services = QueryMiddlewareUtilities
                    .CreateRequestServices(context)
            };
        }

        private static async Task<QueryRequest> ReadRequestAsync(
            HttpContext context)
        {
            using (StreamReader reader = new StreamReader(
                context.Request.Body, Encoding.UTF8))
            {
                string json = await reader.ReadToEndAsync();
                return JsonConvert.DeserializeObject<QueryRequest>(json);
            }
        }
    }
}
