using System;

#if ASPNETCLASSIC
using HotChocolate.AspNetClassic.Voyager;
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
namespace HotChocolate.AspNetClassic.Voyager
#else
namespace HotChocolate.AspNetCore.Voyager
#endif
{
    public static class ApplicationBuilderExtensions
    {
#if ASPNETCLASSIC
        private const string _resourcesNamespace =
            "HotChocolate.AspNetClassic.Voyager.Resources";
#else
        private const string _resourcesNamespace =
            "HotChocolate.AspNetCore.Voyager.Resources";
#endif

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
#if ASPNETCLASSIC
                app => app.Use<SettingsMiddleware>(options));
#else
                app => app.UseMiddleware<SettingsMiddleware>(options));
#endif
        }

#if ASPNETCLASSIC
        private static IApplicationBuilder UseVoyagerFileServer(
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
