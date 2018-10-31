using System;
using System.Reflection;
using HotChocolate.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;

namespace HotChocolate
{
    public static class GraphiQLApplicationBuilderExtensions
    {
        private const string _resourcesNamespace =
            "HotChocolate.AspNetCore.Resources";

        public static void UseGraphiQL(
            this IApplicationBuilder applicationBuilder,
            GraphiQLOptions options)
        {
            applicationBuilder.UseGraphiQLFileServer(options.Route);
            //applicationBuilder.UseGraphiQLSettingsMiddleware(options);
        }

        private static void UseGraphiQLSettingsMiddleware(
           this IApplicationBuilder applicationBuilder,
           GraphiQLOptions options)
        {
            string path = options.Route.TrimEnd('/') + "/settings.js";
            applicationBuilder.Map(path,
                app => app.UseMiddleware<SettingsMiddleware>(options));
        }

        private static void UseGraphiQLFileServer(
            this IApplicationBuilder applicationBuilder,
            string route)
        {
            var fileServerOptions = new FileServerOptions
            {
                RequestPath = route,
                FileProvider = CreateFileProvider(),
                EnableDefaultFiles = true,
                StaticFileOptions =
                {
                    ContentTypeProvider = new FileExtensionContentTypeProvider()
                }
            };

            applicationBuilder.UseFileServer(fileServerOptions);
        }

        private static IFileProvider CreateFileProvider()
        {
            Type type = typeof(GraphiQLApplicationBuilderExtensions);

            return new EmbeddedFileProvider(
                type.Assembly,
                _resourcesNamespace);
        }
    }
}
