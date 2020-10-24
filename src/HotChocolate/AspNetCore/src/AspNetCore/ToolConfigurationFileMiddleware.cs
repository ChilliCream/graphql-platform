using System.Threading;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Utilities;
using HotChocolate.Execution;
using Microsoft.AspNetCore.Http;
using HttpRequestDelegate = Microsoft.AspNetCore.Http.RequestDelegate;
using Microsoft.AspNetCore.StaticFiles;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace HotChocolate.AspNetCore
{
    /// <summary>
    /// This middleware handles the Banana Cake Pop configuration file request.
    /// </summary>
    public class ToolConfigurationFileMiddleware
        : MiddlewareBase
    {
        private static readonly DefaultContractResolver _contractResolver =
            new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            };
        private static readonly JsonSerializerSettings _serializationSettings =
            new JsonSerializerSettings
            {
                ContractResolver = _contractResolver,
                NullValueHandling = NullValueHandling.Ignore,
            };
        private const string _configFile = "/bcp-config.json";
        private readonly IContentTypeProvider _contentTypeProvider;
        private readonly ToolConfiguration _configuration;
        private readonly PathString _matchUrl;

        public ToolConfigurationFileMiddleware(
            HttpRequestDelegate next,
            IRequestExecutorResolver executorResolver,
            IHttpResultSerializer resultSerializer,
            NameString schemaName,
            PathString matchUrl,
            ToolConfiguration configuration)
            : base(next, executorResolver, resultSerializer, schemaName)
        {
            _contentTypeProvider = new FileExtensionContentTypeProvider();
            _matchUrl = matchUrl;
            _configuration = configuration;
        }

        /// <summary>
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task Invoke(HttpContext context)
        {
            if (Helpers.IsGetOrHeadMethod(context.Request.Method) &&
                Helpers.TryMatchPath(context, _matchUrl, false, out PathString subPath) &&
                subPath.Value == _configFile)
            {
                string endpointPath = context.Request.Path.Value!.Replace(_configFile, "");
                string schemaEndpoint = CreateEndpointUri(
                    context.Request.Host.Value,
                    endpointPath,
                    context.Request.IsHttps,
                    false);
                var config = new BananaCakePopConfiguration(schemaEndpoint)
                {
                    DefaultDocument = _configuration.DefaultDocument,
                    EndpointEditable = false,
                };
                ISchema schema = await ExecutorProxy.GetSchemaAsync(context.RequestAborted);

                if (schema.SubscriptionType is { })
                {
                    config.SubscriptionEndpoint = CreateEndpointUri(
                        context.Request.Host.Value,
                        endpointPath,
                        context.Request.IsHttps,
                        true);
                }

                await WriteAsJsonAsync(context.Response, config, context.RequestAborted);
            }
            else
            {
                await NextAsync(context);
            }
        }

        private string CreateEndpointUri(string host, string path, bool isSecure, bool isWebSocket)
        {
            string scheme = isWebSocket ? "ws" : "http";

            scheme = isSecure ? $"{scheme}s" : scheme;

            return $"{scheme}://{host}{path}";
        }

        private Task WriteAsJsonAsync<TValue>(
            HttpResponse response,
            TValue value,
            CancellationToken cancellationToken = default)
        {
            response.ContentType = "application/json; charset=utf-8";
            response.StatusCode = 200;

            string serializedValue = JsonConvert.SerializeObject(value, _serializationSettings);

            return response.WriteAsync(serializedValue, cancellationToken);
        }

        private class BananaCakePopConfiguration
        {
            public BananaCakePopConfiguration(string schemaEndpoint)
            {
                SchemaEndpoint = schemaEndpoint;
            }

            public string SchemaEndpoint { get; }

            public string? SubscriptionEndpoint { get; set; }

            public bool? EndpointEditable { get; set; }

            public string? DefaultDocument { get; set; }
        }
    }
}
