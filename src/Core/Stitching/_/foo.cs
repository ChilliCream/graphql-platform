using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace HotChocolate.Stitching
{
    public class DelegateToRemoteSchemaMiddleware
    {
        private readonly FieldDelegate _next;
        private static readonly NameString _delegateName = "delegate";

        public DelegateToRemoteSchemaMiddleware(FieldDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public async Task InvokeAsync(IMiddlewareContext context)
        {
            IDirective directive = context.Field.Directives[_delegateName]
                .FirstOrDefault();

            if (directive != null)
            {
                // fetch data from remote schema
            }

            await _next.Invoke(context);
        }
    }

    public class RemoteQueryMiddleware
    {
        private readonly JsonSerializerSettings _jsonSettings =
            new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };


        private QueryDelegate _next;

        public RemoteQueryMiddleware(QueryDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public async Task InvokeAsync(IQueryContext context)
        {
            var request = new QueryRequest(context.Request);



            context.Request = request.ToReadOnly();
        }

        private async Task FetchAsync(
            IReadOnlyQueryRequest request,
            HttpClient httpClient)
        {
            RemoteQueryRequest remoteRequest = CreateRemoteRequest(request);

            var content = new StringContent(
                SerializeRemoteRequest(remoteRequest),
                Encoding.UTF8,
                "application/json");

            HttpResponseMessage response =
                await httpClient.PostAsync(string.Empty, content);


        }

        private RemoteQueryRequest CreateRemoteRequest(
            IReadOnlyQueryRequest request)
        {
            return new RemoteQueryRequest
            {
                Query = request.Query,
                OperationName = request.OperationName,
                Variables = request.VariableValues
            };
        }

        private string SerializeRemoteRequest(
            RemoteQueryRequest remoteRequest)
        {
            return JsonConvert.SerializeObject(
                remoteRequest, _jsonSettings);
        }
    }


    internal class RemoteQueryRequest
    {
        public string OperationName { get; set; }
        public string NamedQuery { get; set; }
        public string Query { get; set; }
        public IReadOnlyDictionary<string, object> Variables { get; set; }
    }
}
