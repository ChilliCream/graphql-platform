using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;

namespace HotChocolate.AspNetCore
{
    public static class GraphiQLApplicationBuilderExtensions
    {
        private const string _resources = "Resources";

        public static void AddGraphiQL(
            this IApplicationBuilder applicationBuilder,
            GraphiQLOptions options)
        {
            applicationBuilder.AddGraphiQLFileServer(options.Route);
        }


        private static void AddGraphiQLFileServer(
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
            TypeInfo type = typeof(GraphiQLApplicationBuilderExtensions)
                .GetTypeInfo();

            return new EmbeddedFileProvider(
                type.Assembly,
                $"{type.Namespace}.{_resources}");
        }
    }
}
