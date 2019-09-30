using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Text.Json;
using HotChocolate.Stitching.Introspection;
using McMaster.Extensions.CommandLineUtils;
using HotChocolate.Language;
using IOPath = System.IO.Path;

namespace StrawberryShake.Tools
{
    public class InitCommand
        : ICommand
    {
        [Argument(0, "path", "The directory where the client shall be located.")]
        [Required]
        public string? Path { get; set; }

        [Argument(1, "uri", "The URL to the GraphQL endpoint.")]
        [Required]
        public string? Url { get; set; }

        [Option("-n|--SchemaName")]
        public string? SchemaName { get; set; }

        [Option]
        public string? Token { get; set; }

        [Option]
        public string? Scheme { get; set; }

        public async Task<int> OnExecute()
        {
            if (Path is null)
            {
                throw new InvalidOperationException("Path mustn't not be null.");
            }

            if (Url is null)
            {
                throw new InvalidOperationException("Url mustn't not be null.");
            }

            var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(Url);

            if (Token != null)
            {
                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue(
                        Scheme ?? "bearer", Token);
            }

            var stopwatch = Stopwatch.StartNew();

            Console.WriteLine("Download schema started.");
            DocumentNode schema = await IntrospectionClient.LoadSchemaAsync(httpClient);
            schema = IntrospectionClient.RemoveBuiltInTypes(schema);
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

            using (var stream = File.Create(IOPath.Combine(Path, WellKnownFiles.Config)))
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
            return 0;
        }
    }
}
