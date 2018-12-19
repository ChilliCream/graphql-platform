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
namespace HotChocolate.AspNetClassic.GraphiQL
#else
namespace HotChocolate.AspNetCore.GraphiQL
#endif
{
    public static class ApplicationBuilderExtensions
    {
#if ASPNETCLASSIC
        private const string _resourcesNamespace =
            "HotChocolate.AspNetClassic.GraphiQL.Resources";
#else
        private const string _resourcesNamespace =
            "HotChocolate.AspNetCore.GraphiQL.Resources";
#endif

        public static IApplicationBuilder UseGraphiQL(
            this IApplicationBuilder applicationBuilder)
        {
            return applicationBuilder.UseGraphiQL(new GraphiQLOptions());
        }

        public static IApplicationBuilder UseGraphiQL(
            this IApplicationBuilder applicationBuilder,
            PathString queryPath)
        {
            return applicationBuilder.UseGraphiQL(new GraphiQLOptions
            {
                QueryPath = queryPath,
                Path = queryPath + new PathString("/graphiql")
            });
        }

        public static IApplicationBuilder UseGraphiQL(
            this IApplicationBuilder applicationBuilder,
            PathString queryPath,
            PathString uiPath)
        {
            return applicationBuilder.UseGraphiQL(new GraphiQLOptions
            {
                QueryPath = queryPath,
                Path = uiPath
            });
        }

        public static IApplicationBuilder UseGraphiQL(
            this IApplicationBuilder applicationBuilder,
            GraphiQLOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return applicationBuilder
                .UseGraphiQLSettingsMiddleware(options)
                .UseGraphiQLFileServer(options.Path);
        }

        private static IApplicationBuilder UseGraphiQLSettingsMiddleware(
           this IApplicationBuilder applicationBuilder,
           GraphiQLOptions options)
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
        private static IApplicationBuilder UseGraphiQLFileServer(
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
        private static IApplicationBuilder UseGraphiQLFileServer(
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
