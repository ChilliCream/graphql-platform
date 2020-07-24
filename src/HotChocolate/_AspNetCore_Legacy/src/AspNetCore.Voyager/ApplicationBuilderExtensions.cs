using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;

namespace HotChocolate.AspNetCore.Voyager
{
    public static class ApplicationBuilderExtensions
    {
        private const string _resourcesNamespace =
            "HotChocolate.AspNetCore.Voyager.Resources";

        public static IApplicationBuilder UseVoyager(
            this IApplicationBuilder applicationBuilder)
        {
            return applicationBuilder.UseVoyager(new VoyagerOptions());
        }

        public static IApplicationBuilder UseVoyager(
           this IApplicationBuilder applicationBuilder,
           PathString queryPath)
        {
            return applicationBuilder.UseVoyager(new VoyagerOptions
            {
                QueryPath = queryPath,
                Path = queryPath + new PathString("/voyager")
            });
        }

        public static IApplicationBuilder UseVoyager(
            this IApplicationBuilder applicationBuilder,
            PathString queryPath,
            PathString uiPath)
        {
            return applicationBuilder.UseVoyager(new VoyagerOptions
            {
                QueryPath = queryPath,
                Path = uiPath
            });
        }

        public static IApplicationBuilder UseVoyager(
            this IApplicationBuilder applicationBuilder,
            VoyagerOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return applicationBuilder
                .UseVoyagerSettingsMiddleware(options)
                .UseVoyagerFileServer(options.Path);
        }

        private static IApplicationBuilder UseVoyagerSettingsMiddleware(
           this IApplicationBuilder applicationBuilder,
           VoyagerOptions options)
        {
            return applicationBuilder.Map(
                options.Path.Add(new PathString("/settings.js")),
                app => app.UseMiddleware<SettingsMiddleware>(options));
        }

        private static IApplicationBuilder UseVoyagerFileServer(
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
