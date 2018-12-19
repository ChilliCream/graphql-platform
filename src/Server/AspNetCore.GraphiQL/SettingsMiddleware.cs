using System;
using System.Threading.Tasks;

#if ASPNETCLASSIC
using Microsoft.Owin;
using HttpContext = Microsoft.Owin.IOwinContext;
using HttpRequest = Microsoft.Owin.IOwinRequest;
using RequestDelegate = Microsoft.Owin.OwinMiddleware;
#else
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
#endif

#if ASPNETCLASSIC
namespace HotChocolate.AspNetClassic.GraphiQL
#else
namespace HotChocolate.AspNetCore.GraphiQL
#endif
{
    internal sealed class SettingsMiddleware
#if ASPNETCLASSIC
        : RequestDelegate
#endif
    {
        private readonly GraphiQLOptions _options;
        private readonly string _queryPath;
        private readonly string _subscriptionPath;

        public SettingsMiddleware(
            RequestDelegate next,
            GraphiQLOptions options)
#if ASPNETCLASSIC
                : base(next)
#endif
        {
#if !ASPNETCLASSIC
            Next = next;
#endif
            _options = options
                ?? throw new ArgumentNullException(nameof(options));

            Uri uiPath = UriFromPath(options.Path);
            Uri queryPath = UriFromPath(options.QueryPath);
            Uri subscriptionPath = UriFromPath(options.SubscriptionPath);

            _queryPath = uiPath.MakeRelativeUri(queryPath).ToString();
            _subscriptionPath = uiPath.MakeRelativeUri(subscriptionPath)
                .ToString();
        }

#if !ASPNETCLASSIC
        internal RequestDelegate Next { get; }
#endif

#if ASPNETCLASSIC
        /// <inheritdoc />
        public override async Task Invoke(HttpContext context)
#else
        /// <summary>
        ///
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task InvokeAsync(HttpContext context)
#endif
        {
            string queryUrl = BuildUrl(context.Request, false, _queryPath);
            string subscriptionUrl = BuildUrl(context.Request, true,
                _subscriptionPath);

            context.Response.ContentType = "application/javascript";
            await context.Response.WriteAsync($@"
                window.Settings = {{
                    url: ""{queryUrl}"",
                    subscriptionUrl: ""{subscriptionUrl}"",
                }}
            ", context.GetCancellationToken());
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
                scheme = request.IsHttps() ? "wss" : "ws";
            }

#if ASPNETCLASSIC
            Uri uri = request.Uri;
            var uriBuilder = new UriBuilder(scheme, uri.Host, uri.Port,
                uiPath + path);

            return uriBuilder.ToString().TrimEnd('/');
#else
            return UriHelper.BuildAbsolute(
                scheme, request.Host, uiPath + path)
                .TrimEnd('/');
#endif
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
