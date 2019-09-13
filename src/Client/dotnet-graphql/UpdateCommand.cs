using System.Net;
using System;
using System.Collections.Generic;
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
using System.ComponentModel.DataAnnotations;

namespace StrawberryShake.Tools
{
    public class UpdateCommand
        : ICommand
    {
        [Argument(0, "path")]
        public string Path { get; set; }

        [Option]
        public string Token { get; set; }

        [Option]
        public string Scheme { get; set; }

        public async Task<int> OnExecute()
        {
            if (Path is null)
            {
                foreach (string configFile in Directory.GetFiles(
                    Environment.CurrentDirectory,
                    "config.json",
                    SearchOption.AllDirectories))
                {
                    string directory = IOPath.GetDirectoryName(configFile);
                    if (Directory.GetFiles(
                        directory,
                        "*.graphql").Length > 0)
                    {
                        Configuration config = null;
                        try
                        {
                            config = await Configuration.LoadConfig(directory);
                        }
                        catch
                        {
                            // ignore invalid configs
                        }

                        if (config != null && config.Schemas.Count > 0)
                        {
                            await UpdateSchemaAsync(directory, config);
                        }
                    }
                }
            }
            else
            {
                Configuration config = await Configuration.LoadConfig(
                    IOPath.Combine(Path, "config.json"));
                await UpdateSchemaAsync(Path, config);
            }
            return 0;
        }


        private async Task UpdateSchemaAsync(string path, Configuration configuration)
        {
            foreach (SchemaFile schema in configuration.Schemas)
            {
                if (schema.Type == "http")
                {
                    await UpdateSchemaAsync(path, schema);
                }
            }
        }

        private async Task UpdateSchemaAsync(string path, SchemaFile schemaFile)
        {
            var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(schemaFile.Url);

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
                $"{stopwatch.ElapsedMilliseconds} ms for {path}.");

            stopwatch.Restart();
            Console.WriteLine("Client configuration started.");

            string fileName = IOPath.Combine(path, schemaFile.Name);
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }

            using (var stream = File.Create(fileName))
            {
                using (var sw = new StreamWriter(stream))
                {
                    SchemaSyntaxSerializer.Serialize(schema, sw, true);
                }
            }

            Console.WriteLine(
                "Client configuration completed in " +
                $"{stopwatch.ElapsedMilliseconds} ms for {path}.");
        }
    }
}
