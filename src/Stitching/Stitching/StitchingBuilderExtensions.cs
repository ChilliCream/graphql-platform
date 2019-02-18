using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Stitching.Introspection;
using HotChocolate.Stitching.Properties;
using HotChocolate.Stitching.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Stitching
{
    public static class StitchingBuilderExtensions
    {
        private static readonly string _introspectionQuery =
            Resources.IntrospectionQuery;

        public static IStitchingBuilder AddSchemaFromString(
            this IStitchingBuilder builder,
            NameString name,
            string schema)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (string.IsNullOrEmpty(schema))
            {
                throw new ArgumentException(
                    "The schema mustn't be null or empty,",
                    nameof(schema));
            }

            name.EnsureNotEmpty(nameof(name));

            builder.AddSchema(name, s => Parser.Default.Parse(schema));
            return builder;
        }

        public static IStitchingBuilder AddSchemaFromFile(
            this IStitchingBuilder builder,
            NameString name,
            string path)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException(
                    "The schema file path mustn't be null or empty,",
                    nameof(path));
            }

            name.EnsureNotEmpty(nameof(name));

            builder.AddSchema(name, s =>
                Parser.Default.Parse(
                    File.ReadAllText(path)));
            return builder;
        }

        public static IStitchingBuilder AddSchemaFromHttp(
            this IStitchingBuilder builder,
            NameString name)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            name.EnsureNotEmpty(nameof(name));

            builder.AddSchema(name, s =>
            {
                HttpClient httpClient =
                    s.GetRequiredService<IHttpClientFactory>()
                    .CreateClient(name);

                var request = new RemoteQueryRequest
                {
                    Query = _introspectionQuery
                };

                var queryClient = new HttpQueryClient();
                string result = Task.Factory.StartNew(
                    () => queryClient.FetchStringAsync(request, httpClient))
                    .Unwrap().GetAwaiter().GetResult();
                return IntrospectionDeserializer.Deserialize(result);
            });

            return builder;
        }

        public static IStitchingBuilder AddExtensionsFromFile(
            this IStitchingBuilder builder,
            string path)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException(
                    "The schema file path mustn't be null or empty,",
                    nameof(path));
            }

            builder.AddExtensions(s =>
                Parser.Default.Parse(
                    File.ReadAllText(path)));
            return builder;
        }

        public static IStitchingBuilder AddExtensionsFromString(
            this IStitchingBuilder builder,
            string extensions)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (string.IsNullOrEmpty(extensions))
            {
                throw new ArgumentException(
                    "The extensions mustn't be null or empty,",
                    nameof(extensions));
            }

            builder.AddExtensions(s => Parser.Default.Parse(extensions));
            return builder;
        }
    }
}
