using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace HotChocolate.AspNetCore
{
    internal static class PostRequest
    {
        private const string _method = "Post";

        internal static bool IsPost(this HttpRequest request)
        {
            return request.Method.Equals(_method, StringComparison.OrdinalIgnoreCase);
        }

        internal static async Task<QueryRequest> ReadRequestAsync(HttpContext context)
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
