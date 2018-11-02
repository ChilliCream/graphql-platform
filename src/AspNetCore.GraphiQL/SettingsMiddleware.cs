using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;

namespace HotChocolate.AspNetCore
{
    internal sealed class SettingsMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly GraphiQLOptions _options;

        private readonly string _queryPath;
        private readonly string _subscriptionPath;

        public SettingsMiddleware(RequestDelegate next, GraphiQLOptions options)
        {
            _next = next
                ?? throw new ArgumentNullException(nameof(next));
            _options = options
                ?? throw new ArgumentNullException(nameof(options));

            var uiPath = UriFromPath(options.Route);
            var queryPath = UriFromPath(options.QueryRoute);
            var subscriptionPath = UriFromPath(options.SubscriptionRoute);

            _queryPath = uiPath.MakeRelativeUri(queryPath).ToString();
            _subscriptionPath = uiPath.MakeRelativeUri(subscriptionPath)
                .ToString();
        }

        public async Task InvokeAsync(HttpContext context)
        {
            string queryUrl = BuildUrl(context.Request, false, _queryPath);
            string subscriptionUrl =
                BuildUrl(context.Request, true, _subscriptionPath);

            context.Response.ContentType = "application/javascript";

            await context.Response.WriteAsync(
                "window.Settings = {",
                context.RequestAborted);

            await context.Response.WriteAsync(
                $"url: \"{queryUrl}\",",
                context.RequestAborted);

            await context.Response.WriteAsync(
                $"subscriptionUrl: \"{subscriptionUrl}\"",
                context.RequestAborted);

            await context.Response.WriteAsync(
                "};",
                context.RequestAborted);
        }

        private static Uri UriFromPath(PathString path)
        {
            return new Uri(
                "http://p" +
                (path.HasValue ? (string)path : "/").TrimEnd('/') +
                "/");
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
    }
}
