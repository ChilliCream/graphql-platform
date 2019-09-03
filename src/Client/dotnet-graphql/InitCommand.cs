using System.Diagnostics;
using System.IO;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using HotChocolate.Stitching.Introspection;
using McMaster.Extensions.CommandLineUtils;
using HotChocolate.Language;
using System.Net.Http.Headers;
using IOPath = System.IO.Path;
using System.Text.Json;
using System.Collections.Generic;

namespace StrawberryShake.Tools
{
    public class InitCommand
        : ICommand
    {
        [Argument(0, "path", "The directory where the client shall be located.")]
        public string Path { get; set; }

        [Argument(2, "uri", "The URL to the GraphQL endpoint.")]
        public string Url { get; set; }

        [Argument(3, "schemaName", "The name of the schema.")]
        public string SchemaName { get; set; }

        [Argument(4, "token", "The authorization token.")]
        public string Token { get; set; }

        [Argument(5, "schema", "The authorization scheme (default: bearer).")]
        public string Scheme { get; set; }

        public async Task<int> OnExecute()
        {
            var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(Url);

            if (Token != null)
            {
                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue(
                        Scheme, Token);
            }

            var stopwatch = Stopwatch.StartNew();

            Console.WriteLine("Download schema started.");
            DocumentNode schema = await IntrospectionClient.LoadSchemaAsync(httpClient);
            Console.WriteLine(
                "Download schema completed in " +
                $"{stopwatch.ElapsedMilliseconds} ms.");

            stopwatch.Restart();
            Console.WriteLine("Client configuration started.");

            if (!Directory.Exists(Path))
            {
                Directory.CreateDirectory(Path);
            }

            SchemaName = SchemaName ?? "schema";
            string schemaFielName = SchemaName + ".graphql";

            var configuration = new Configuration();
            configuration.ClientName = SchemaName + "Client";
            configuration.Schemas = new List<SchemaFile>();
            configuration.Schemas.Add(new SchemaFile
            {
                Type = "http",
                Name = SchemaName,
                File = schemaFielName,
                Url = Url
            });

            using (var stream = File.Create(IOPath.Combine(Path, "config.json")))
            {
                await JsonSerializer.SerializeAsync(stream, configuration);
            }

            using (var stream = File.Create(IOPath.Combine(Path, schemaFielName)))
            {
                using (var sw = new StreamWriter(stream))
                {
                    SchemaSyntaxSerializer.Serialize(schema, sw, true);
                }
            }

            Console.WriteLine(
                "Client configuration completed in " +
                $"{stopwatch.ElapsedMilliseconds} ms for {Path}.");
            return 1;
        }
    }
}
