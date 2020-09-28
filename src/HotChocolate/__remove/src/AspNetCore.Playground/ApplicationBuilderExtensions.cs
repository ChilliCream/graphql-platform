using System;
using HotChocolate.AspNetCore.Playground;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;

namespace HotChocolate.AspNetCore
{
    public static class ApplicationBuilderExtensions
    {
        private const string _resourcesNamespace =
            "HotChocolate.AspNetCore.Playground.Resources";

        public static IApplicationBuilder UsePlayground(
            this IApplicationBuilder applicationBuilder)
        {
            return applicationBuilder.UsePlayground(new PlaygroundOptions());
        }

        public static IApplicationBuilder UsePlayground(
            this IApplicationBuilder applicationBuilder,
            PathString queryPath)
        {
            return applicationBuilder.UsePlayground(new PlaygroundOptions
            {
                QueryPath = queryPath,
                Path = queryPath + new PathString("/playground")
            });
        }

        public static IApplicationBuilder UsePlayground(
            this IApplicationBuilder applicationBuilder,
            PathString queryPath,
            PathString uiPath)
        {
            return applicationBuilder.UsePlayground(new PlaygroundOptions
            {
                QueryPath = queryPath,
                Path = uiPath
            });
        }

        public static IApplicationBuilder UsePlayground(
            this IApplicationBuilder applicationBuilder,
            PlaygroundOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return applicationBuilder
                .UsePlaygroundSettingsMiddleware(options)
                .UsePlaygroundFileServer(options.Path);
        }

        private static IApplicationBuilder UsePlaygroundSettingsMiddleware(
           this IApplicationBuilder applicationBuilder,
           PlaygroundOptions options)
        {
            return applicationBuilder.Map(
                options.Path.Add(new PathString("/settings.js")),
                app => app.UseMiddleware<SettingsMiddleware>(options));
        }

        private static IApplicationBuilder UsePlaygroundFileServer(
            this IApplicationBuilder applicationBuilder,
            string path)
        {
            var fileServerOptions = new FileServerOptions
            {
                RequestPath = path,
                FileProvider = CreateFileProvider(),
                EnableDefaultFiles = true,
                StaticFileOptions =
                {
                    ContentTypeProvider =
                        new FileExtensionContentTypeProvider()
                }
            };

            return applicationBuilder.UseFileServer(fileServerOptions);
        }

        private static IFileProvider CreateFileProvider()
        {
            Type type = typeof(ApplicationBuilderExtensions);

            return new EmbeddedFileProvider(
                type.Assembly,
                _resourcesNamespace);
        }
    }
}
