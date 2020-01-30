using System;
using HotChocolate.AspNetCore.GraphiQL;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;

namespace HotChocolate.AspNetCore
{
    public static class ApplicationBuilderExtensions
    {
        private const string _resourcesNamespace =
            "HotChocolate.AspNetCore.GraphiQL.Resources";

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
                app => app.UseMiddleware<SettingsMiddleware>(options));
        }

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

        private static IFileProvider CreateFileProvider()
        {
            Type type = typeof(ApplicationBuilderExtensions);

            return new EmbeddedFileProvider(
                type.Assembly,
                _resourcesNamespace);
        }
    }
}
