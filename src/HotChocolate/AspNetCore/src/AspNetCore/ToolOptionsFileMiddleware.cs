using Microsoft.AspNetCore.Http;
using HttpRequestDelegate = Microsoft.AspNetCore.Http.RequestDelegate;

namespace HotChocolate.AspNetCore;

/// <summary>
/// This middleware handles the Banana Cake Pop configuration file request.
/// </summary>
public sealed class ToolOptionsFileMiddleware
{
    private const string _configFile = "/bcp-config.json";
    private readonly HttpRequestDelegate _next;
    private readonly PathString _matchUrl;
    private BananaCakePopConfiguration? _config;

    public ToolOptionsFileMiddleware(HttpRequestDelegate next, PathString matchUrl)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _matchUrl = matchUrl;
    }

    public async Task Invoke(HttpContext context)
    {
        if (context.Request.IsGetOrHeadMethod() &&
            context.Request.TryMatchPath(_matchUrl, false, out var subPath) &&
            subPath.Value == _configFile &&
            (context.GetGraphQLToolOptions()?.Enable ?? true))
        {
            if (_config is null)
            {
                var options = context.GetGraphQLToolOptions();
                var endpointOptions = context.GetGraphQLEndpointOptions();

                var config = new BananaCakePopConfiguration();

                if (endpointOptions is not null)
                {
                    config.UseBrowserUrlAsEndpoint = true;
                    config.Endpoint = endpointOptions.GraphQLEndpoint;
                }

                if (options is not null)
                {
                    config.Title = options.Title;
                    config.GraphQLDocument = options.Document;
                    config.UseBrowserUrlAsEndpoint = options.UseBrowserUrlAsGraphQLEndpoint;

                    if (options.GraphQLEndpoint is not null)
                    {
                        config.Endpoint = options.GraphQLEndpoint;
                    }

                    config.IncludeCookies = options.IncludeCookies;
                    config.HttpHeaders = ConvertHttpHeadersToDictionary(options.HttpHeaders);
                    config.UseGet = ConvertHttpMethodToString(options.HttpMethod);
                    config.GaTrackingId = options.GaTrackingId;
                    config.DisableTelemetry = options.DisableTelemetry;
                }

                _config = config;
            }

            await context.Response.WriteAsJsonAsync(_config, context.RequestAborted);
        }
        else
        {
            await _next(context);
        }
    }

    private static IDictionary<string, string>? ConvertHttpHeadersToDictionary(
        IHeaderDictionary? httpHeaders)
    {
        if (httpHeaders is not null)
        {
            var result = new Dictionary<string, string>();

            foreach ((var key, var value) in httpHeaders)
            {
                result.Add(key, value.ToString());
            }

            return result;
        }

        return null;
    }

    private bool? ConvertHttpMethodToString(DefaultHttpMethod? httpMethod)
        => httpMethod switch
        {
            DefaultHttpMethod.Get => true,
            DefaultHttpMethod.Post => false,
            _ => null
        };

    private sealed class BananaCakePopConfiguration
    {
        public string? Title { get; set; }

        public bool UseBrowserUrlAsEndpoint { get; set; } = true;

        public string? Endpoint { get; set; }

        public string? GraphQLDocument { get; set; }

        public bool? IncludeCookies { get; set; }

        public IDictionary<string, string>? HttpHeaders { get; set; }

        public bool? UseGet { get; set; }

        public string? GaTrackingId { get; set; }

        public bool? DisableTelemetry { get; set; }
    }
}
