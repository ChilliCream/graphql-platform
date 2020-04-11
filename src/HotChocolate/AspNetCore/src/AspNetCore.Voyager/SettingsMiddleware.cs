using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;

namespace HotChocolate.AspNetCore.Voyager
{
    internal sealed class SettingsMiddleware
    {
        private readonly VoyagerOptions _options;
        private readonly string _queryPath;

        public SettingsMiddleware(
            RequestDelegate next,
            VoyagerOptions options)
        {
            Next = next;
            _options = options
                ?? throw new ArgumentNullException(nameof(options));

            Uri uiPath = UriFromPath(options.Path);
            Uri queryPath = UriFromPath(options.QueryPath);

            _queryPath = uiPath.MakeRelativeUri(queryPath).ToString();
        }

        internal RequestDelegate Next { get; }

        /// <summary>
        ///
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task InvokeAsync(HttpContext context)
        {
            string queryUrl = _options.GraphQLEndpoint?.AbsoluteUri
                ?? BuildUrl(context.Request, _queryPath);

            context.Response.ContentType = "application/javascript";

            await context.Response.WriteAsync($@"
                window.Settings = {{
                    url: ""{queryUrl}"",
                }}
            ",
            context.GetCancellationToken())
            .ConfigureAwait(false);
        }

        private static string BuildUrl(
            HttpRequest request,
            string path)
        {
            string uiPath = request.PathBase.Value
                .Substring(0, request.PathBase.Value.Length - 11);
            string scheme = request.Scheme;

            return UriHelper.BuildAbsolute(
                scheme, request.Host, uiPath + path)
                .TrimEnd('/');
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
