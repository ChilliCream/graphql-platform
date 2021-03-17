using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Net.Http.Headers;

namespace HotChocolate.AspNetCore
{
    /// <summary>
    /// This examines a directory path and determines if there is a default file present.
    /// If so the file name is appended to the path and execution continues.
    /// Note we don't just serve the file because it may require interpretation.
    /// </summary>
    public class ToolDefaultFileMiddleware
    {
        private const string _defaultFile = "index.html";
        private readonly IFileProvider _fileProvider;
        private readonly PathString _matchUrl;
        private readonly RequestDelegate _next;

        /// <summary>
        /// Creates a new instance of the DefaultFilesMiddleware.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        /// <param name="fileProvider">The <see cref="IFileProvider"/> used by this middleware.</param>
        /// <param name="matchUrl">The match url.</param>
        public ToolDefaultFileMiddleware(
            RequestDelegate next,
            IFileProvider fileProvider,
            PathString matchUrl)
        {
            if (next is null)
            {
                throw new ArgumentNullException(nameof(next));
            }

            if (fileProvider is null)
            {
                throw new ArgumentNullException(nameof(fileProvider));
            }

            _next = next;
            _fileProvider = fileProvider;
            _matchUrl = matchUrl;
        }

        /// <summary>
        /// This examines the request to see if it matches a configured directory, and if there are any files with the
        /// configured default names in that directory.  If so this will append the corresponding file name to the request
        /// path for a later middleware to handle.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public Task Invoke(HttpContext context)
        {
            if (context.Request.IsGetOrHeadMethod() &&
                context.Request.AcceptHeaderContainsHtml() &&
                context.Request.TryMatchPath(_matchUrl, true, out PathString subPath) &&
                (context.GetGraphQLToolOptions()?.Enable ?? true))
            {
                var dirContents = _fileProvider.GetDirectoryContents(subPath.Value);

                if (dirContents.Exists)
                {
                    // Check if any of our default files exist.
                    var file = _fileProvider.GetFileInfo(subPath.Value + _defaultFile);

                    // TryMatchPath will make sure subpath always ends with a "/" by adding it if needed.
                    if (file.Exists)
                    {
                        // If the path matches a directory but does not end in a slash, redirect to add the slash.
                        // This prevents relative links from breaking.
                        if (!context.Request.PathEndsInSlash())
                        {
                            context.Response.StatusCode = StatusCodes.Status301MovedPermanently;

                            var request = context.Request;
                            var redirect = UriHelper.BuildAbsolute(request.Scheme, request.Host,
                                request.PathBase, request.Path + "/", request.QueryString);

                            context.Response.Headers[HeaderNames.Location] = redirect;

                            return Task.CompletedTask;
                        }

                        // Match found, re-write the url. A later middleware will actually serve the file.
                        context.Request.Path =
                            new PathString(context.Request.Path.Value + _defaultFile);
                    }
                }
            }

            return _next(context);
        }
    }
}
