using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.Stitching.Introspection;
using McMaster.Extensions.CommandLineUtils;
using IOPath = System.IO.Path;
using HCErrorBuilder = HotChocolate.ErrorBuilder;

namespace StrawberryShake.Tools
{
    public static class InitCommand
    {
        public static CommandLineApplication Create()
        {
            var init = new CommandLineApplication();
            init.AddName("init");
            init.AddHelp<InitHelpTextGenerator>();

            CommandArgument urlArg = init.Argument(
                "uri",
                "The URL to the GraphQL endpoint.",
                c => c.IsRequired());

            CommandOption pathArg = init.Option(
                "-p|--Path",
                "The directory where the client shall be located.",
                CommandOptionType.SingleOrNoValue);

            CommandOption schemaArg = init.Option(
                "-n|--SchemaName",
                "The schema name.",
                CommandOptionType.SingleOrNoValue);

            CommandOption tokenArg = init.Option(
                "-t|--token",
                "The token that shall be used to autheticate with the GraphQL server.",
                CommandOptionType.SingleOrNoValue);

            CommandOption schemeArg = init.Option(
                "-s|--scheme",
                "The token scheme (defaul: bearer).",
                CommandOptionType.SingleOrNoValue);

            init.OnExecuteAsync(async cancellationToken =>
            {
                string path = pathArg.HasValue()
                    ? pathArg.Value()!
                    : Environment.CurrentDirectory;

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                string schemaName = (schemaArg.HasValue()
                    ? schemaArg.Value()!
                    : "schema").Trim();
                string schemaFielName = schemaName + ".graphql";

                if (!await DownloadSchemaAsync(
                    urlArg, tokenArg, schemaArg, schemaFielName, path))
                {
                    return 1;
                }

                await WriteConfigurationAsync(
                    schemaName, schemaFielName, path, urlArg, cancellationToken);

                return 0;
            });

            return init;
        }

        private static async Task<bool> DownloadSchemaAsync(
            CommandArgument urlArg,
            CommandOption tokenArg,
            CommandOption schemeArg,
            string schemaFielName,
            string path)
        {
            try
            {
                var httpClient = new HttpClient();
                httpClient.BaseAddress = new Uri(urlArg.Value);
                httpClient.DefaultRequestHeaders.UserAgent.Add(
                    new ProductInfoHeaderValue(
                        new ProductHeaderValue(
                            "StrawberryShake",
                            typeof(InitCommand).Assembly!.GetName()!.Version!.ToString())));

                if (tokenArg.HasValue())
                {
                    httpClient.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue(schemeArg.Value() ?? "bearer", tokenArg.Value());
                }

                var stopwatch = Stopwatch.StartNew();

                Console.WriteLine("Download schema started.");
                DocumentNode schema = await IntrospectionClient.LoadSchemaAsync(httpClient);
                schema = IntrospectionClient.RemoveBuiltInTypes(schema);

                using (var stream = File.Create(IOPath.Combine(path, schemaFielName)))
                {
                    using (var sw = new StreamWriter(stream))
                    {
                        SchemaSyntaxSerializer.Serialize(schema, sw, true);
                    }
                }

                Console.WriteLine(
                    "Download schema completed in " +
                    $"{stopwatch.ElapsedMilliseconds} ms.");

                stopwatch.Stop();
                return true;
            }
            catch (HttpRequestException ex)
            {
                HCErrorBuilder.New()
                    .SetMessage(ex.Message)
                    .SetCode("HTTP_ERROR")
                    .Build()
                    .Write();

                return false;
            }
        }

        private static async Task WriteConfigurationAsync(
            string schemaName,
            string schemaFielName,
            string path,
            CommandArgument urlArg,
            CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            Console.WriteLine("Client configuration started.");

            var configuration = new Configuration();
            configuration.ClientName = schemaName + "Client";
            configuration.Schemas = new List<SchemaFile>();
            configuration.Schemas.Add(new SchemaFile
            {
                Type = "http",
                Name = schemaName,
                File = schemaFielName,
                Url = urlArg.Value?.Trim()
            });

            using (var stream = File.Create(IOPath.Combine(path, WellKnownFiles.Config)))
            {
                await JsonSerializer.SerializeAsync(
                    stream, configuration, cancellationToken: cancellationToken);
            }

            Console.WriteLine(
                "Client configuration completed in " +
                $"{stopwatch.ElapsedMilliseconds} ms for {path}.");

            stopwatch.Stop();
        }
    }
}
