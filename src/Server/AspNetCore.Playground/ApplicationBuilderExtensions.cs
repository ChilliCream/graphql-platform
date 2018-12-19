using System;

#if ASPNETCLASSIC
using Microsoft.Owin;
using Microsoft.Owin.FileSystems;
using Microsoft.Owin.StaticFiles;
using Microsoft.Owin.StaticFiles.ContentTypes;
using Owin;
using IApplicationBuilder = Owin.IAppBuilder;
#else
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
#endif

#if ASPNETCLASSIC
namespace HotChocolate.AspNetClassic.Playground
#else
namespace HotChocolate.AspNetCore.Playground
#endif
{
    public static class ApplicationBuilderExtensions
    {
#if ASPNETCLASSIC
        private const string _resourcesNamespace =
            "HotChocolate.AspNetClassic.Playground.Resources";
#else
        private const string _resourcesNamespace =
            "HotChocolate.AspNetCore.Playground.Resources";
#endif

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
#if ASPNETCLASSIC
                app => app.Use<SettingsMiddleware>(options));
#else
                app => app.UseMiddleware<SettingsMiddleware>(options));
#endif
        }

#if ASPNETCLASSIC
        private static IApplicationBuilder UsePlaygroundFileServer(
            this IApplicationBuilder applicationBuilder,
            PathString route)
        {
            var fileServerOptions = new FileServerOptions
            {
                RequestPath = route,
                FileSystem = CreateFileSystem(),
                EnableDefaultFiles = true,
                StaticFileOptions =
                {
                    ContentTypeProvider =
                        new FileExtensionContentTypeProvider()
                }
            };

            return applicationBuilder.UseFileServer(fileServerOptions);
        }
#else
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
#endif

#if ASPNETCLASSIC
        private static IFileSystem CreateFileSystem()
        {
            Type type = typeof(ApplicationBuilderExtensions);

            return new EmbeddedResourceFileSystem(
                type.Assembly,
                _resourcesNamespace);
        }
#else
        private static IFileProvider CreateFileProvider()
        {
            Type type = typeof(ApplicationBuilderExtensions);

            return new EmbeddedFileProvider(
                type.Assembly,
                _resourcesNamespace);
        }
#endif
    }
}
