using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;
using HotChocolate.AspNetCore.Utilities;
using HotChocolate.Execution;
using Microsoft.AspNetCore.Http;
using HttpRequestDelegate = Microsoft.AspNetCore.Http.RequestDelegate;

namespace HotChocolate.AspNetCore
{
    /// <summary>
    /// This middleware handles the Banana Cake Pop configuration file request.
    /// </summary>
    public class ToolOptionsFileMiddleware
        : MiddlewareBase
    {
        private const string _configFile = "/bcp-config.json";
        private readonly PathString _matchUrl;

        public ToolOptionsFileMiddleware(
            HttpRequestDelegate next,
            IRequestExecutorResolver executorResolver,
            IHttpResultSerializer resultSerializer,
            NameString schemaName,
            PathString matchUrl)
            : base(next, executorResolver, resultSerializer, schemaName)
        {
            _matchUrl = matchUrl;
        }

        /// <summary>
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task Invoke(HttpContext context)
        {
            if (context.Request.IsGetOrHeadMethod() &&
                context.Request.TryMatchPath(_matchUrl, false, out PathString subPath) &&
                subPath.Value == _configFile &&
                (context.GetGraphQLToolOptions()?.Enable ?? true))
            {
                GraphQLToolOptions? options = context.GetGraphQLToolOptions();
                string endpointPath = context.Request.Path.Value!.Replace(_configFile, "");
                string schemaEndpoint = CreateEndpointUri(
                    context.Request.Host.Value,
                    endpointPath,
                    context.Request.IsHttps,
                    false);
                var config = new BananaCakePopConfiguration(schemaEndpoint)
                {
                    EndpointEditable = true,
                };
                ISchema schema = await ExecutorProxy.GetSchemaAsync(context.RequestAborted);

                if (options is { })
                {
                    config.Document = options.Document;
                    config.Credentials = ConvertCredentialsToString(options.Credentials);
                    config.HttpHeaders = ConvertHttpHeadersToDictionary(options.HttpHeaders);
                    config.HttpMethod = ConvertHttpMethodToString(options.HttpMethod);
                }

                if (schema.SubscriptionType is { })
                {
                    config.SubscriptionEndpoint = CreateEndpointUri(
                        context.Request.Host.Value,
                        endpointPath,
                        context.Request.IsHttps,
                        true);
                }

                await context.Response.WriteAsJsonAsync(config, context.RequestAborted);
            }
            else
            {
                await NextAsync(context);
            }
        }

        private static string? ConvertCredentialsToString(DefaultCredentials? credentials)
        {
            if (credentials is not null)
            {
                switch (credentials)
                {
                    case DefaultCredentials.Include:
                        return "include";
                    case DefaultCredentials.Omit:
                        return "omit";
                    case DefaultCredentials.SameOrigin:
                        return "same-origin";
                }
            }

            return null;
        }

        private static IDictionary<string, string>? ConvertHttpHeadersToDictionary(
            IHeaderDictionary? httpHeaders)
        {
            if (httpHeaders is not null)
            {
                var result = new Dictionary<string, string>();

                foreach ((var key, StringValues value) in httpHeaders)
                {
                    result.Add(key, value.ToString());
                }

                return result;
            }

            return null;
        }

        private string? ConvertHttpMethodToString(DefaultHttpMethod? httpMethod)
        {
            if (httpMethod is not null)
            {
                switch (httpMethod)
                {
                    case DefaultHttpMethod.Get:
                        return "GET";
                    case DefaultHttpMethod.Post:
                        return "POST";
                }
            }

            return null;
        }

        private string CreateEndpointUri(string host, string path, bool isSecure, bool isWebSocket)
        {
            string scheme = isWebSocket ? "ws" : "http";

            scheme = isSecure ? $"{scheme}s" : scheme;

            return $"{scheme}://{host}{path}";
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

            public string? Document { get; set; }

            public string? Credentials { get; set; }

            public IDictionary<string, string>? HttpHeaders { get; set; }

            public string? HttpMethod { get; set; }
        }
    }
}
