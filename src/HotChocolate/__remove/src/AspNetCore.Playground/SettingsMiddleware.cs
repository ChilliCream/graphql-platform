using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;

namespace HotChocolate.AspNetCore.Playground
{
    internal sealed class SettingsMiddleware
    {
        private readonly PlaygroundOptions _options;
        private readonly string _queryPath;
        private readonly string _subscriptionPath;

        public SettingsMiddleware(
            RequestDelegate next,
            PlaygroundOptions options)
        {
            Next = next;
            _options = options
                ?? throw new ArgumentNullException(nameof(options));

            Uri uiPath = UriFromPath(options.Path);
            Uri queryPath = UriFromPath(options.QueryPath);
            Uri subscriptionPath = UriFromPath(options.SubscriptionPath);

            _queryPath = uiPath.MakeRelativeUri(queryPath).ToString();
            _subscriptionPath = uiPath.MakeRelativeUri(subscriptionPath)
                .ToString();
        }

        internal RequestDelegate Next { get; }

        /// <summary>
        ///
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task InvokeAsync(HttpContext context)
        {
            string queryUrl = BuildUrl(_options.GraphQLEndpoint, context.Request, false, _queryPath);

            string subscriptionUrl = _options.EnableSubscription
                ? $"\"{BuildUrl(_options.GraphQLEndpoint, context.Request, true, _subscriptionPath)}\""
                : "null";

            context.Response.ContentType = "application/javascript";

            await context.Response.WriteAsync($@"
                window.Settings = {{
                    url: ""{queryUrl}"",
                    subscriptionUrl: {subscriptionUrl},
                }}
            ",
            context.RequestAborted)
            .ConfigureAwait(false);
        }

        private static string BuildUrl(
            HttpRequest request,
            bool websocket,
            string path)
        {
            string uiPath = request.PathBase.Value
                .Substring(0, request.PathBase.Value.Length - 11);
            string scheme = request.Scheme;

            if (websocket)
            {
                scheme = request.IsHttps ? "wss" : "ws";
            }

            return UriHelper.BuildAbsolute(
                scheme, request.Host, uiPath + path)
                .TrimEnd('/');
        }

        private static string BuildUrl(Uri? uri, HttpRequest request, bool websocket, string path)
        {
            if (uri is null)
                return BuildUrl(request, websocket, path);

            if (!websocket)
                return uri.AbsoluteUri;

            var builder = new UriBuilder(uri)
            {
                Scheme = uri.Scheme == Uri.UriSchemeHttps ? "wss" : "ws"
            };

            return builder.Uri.AbsoluteUri;
        }

        private static Uri UriFromPath(PathString path)
        {
            return new Uri(
                "http://p" +
                (path.HasValue ? path.Value : "/").TrimEnd('/') +
                "/");
        }
    }
}
