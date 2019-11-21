using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using HotChocolate.Stitching.Introspection;
using HotChocolate.Language;
using IOPath = System.IO.Path;
using HCErrorBuilder = HotChocolate.ErrorBuilder;

namespace StrawberryShake.Tools
{
    public static class UpdateCommand
    {
        public static CommandLineApplication Create()
        {
            var init = new CommandLineApplication();
            init.AddName("update");
            init.AddHelp<UpdateHelpTextGenerator>();

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

            CommandOption urlArg = init.Option(
                "-u|--uri",
                "The URL to the GraphQL endpoint.",
                CommandOptionType.SingleOrNoValue);

            init.OnExecuteAsync(async cancellationToken =>
            {
                if (pathArg.HasValue())
                {
                    await FindAndUpdateSchemasAsync(
                        urlArg, tokenArg, schemeArg);
                }
                else
                {
                    await UpdateSingleSchemaAsync(
                        pathArg.Value()!, urlArg, tokenArg, schemeArg);
                }
                return 0;
            });

            return init;
        }

        private static async Task<bool> FindAndUpdateSchemasAsync(
            CommandOption urlArg,
            CommandOption tokenArg,
            CommandOption schemeArg)
        {
            bool hasErrors = false;

            foreach (string configFile in Directory.GetFiles(
                Environment.CurrentDirectory,
                WellKnownFiles.Config,
                SearchOption.AllDirectories))
            {
                string directory = IOPath.GetDirectoryName(configFile)!;
                if (Directory.GetFiles(directory, "*.graphql").Length > 0)
                {
                    try
                    {
                        if (!await UpdateSingleSchemaAsync(
                            directory, urlArg, tokenArg, schemeArg))
                        {
                            hasErrors = true;
                        }
                    }
                    catch
                    {
                        hasErrors = true;
                    }
                }
            }

            return hasErrors;
        }

        private static async Task<bool> UpdateSingleSchemaAsync(
            string path,
            CommandOption urlArg,
            CommandOption tokenArg,
            CommandOption schemeArg)
        {
            Configuration? config = await Configuration.LoadConfig(
                IOPath.Combine(path, WellKnownFiles.Config));

            if (config is { })
            {
                return await UpdateSchemaAsync(path, config, urlArg, tokenArg, schemeArg);
            }

            return false;
        }

        private static async Task<bool> UpdateSchemaAsync(
            string path,
            Configuration configuration,
            CommandOption urlArg,
            CommandOption tokenArg,
            CommandOption schemeArg)
        {
            bool hasErrors = false;

            foreach (SchemaFile schema in configuration.Schemas!)
            {
                if (schema.Type == "http")
                {
                    if (!await UpdateSchemaAsync(path, schema, urlArg, tokenArg, schemeArg))
                    {
                        hasErrors = true;
                    }
                }
            }

            return hasErrors;
        }

        private static async Task<bool> UpdateSchemaAsync(
            string path,
            SchemaFile schemaFile,
            CommandOption urlArg,
            CommandOption tokenArg,
            CommandOption schemeArg)
        {
            var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(
                urlArg.HasValue()
                    ? urlArg.Value()
                    : schemaFile.Url);

            if (tokenArg.HasValue())
            {
                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue(
                        schemeArg.HasValue() ? schemeArg.Value() : "bearer",
                        tokenArg.Value());
            }

            try
            {
                var stopwatch = Stopwatch.StartNew();

                Console.WriteLine("Download schema started.");
                DocumentNode schema = await IntrospectionClient.LoadSchemaAsync(httpClient);
                schema = IntrospectionClient.RemoveBuiltInTypes(schema);
                Console.WriteLine(
                    "Download schema completed in " +
                    $"{stopwatch.ElapsedMilliseconds} ms for {path}.");

                stopwatch.Restart();
                Console.WriteLine("Client configuration started.");

                string fileName = IOPath.Combine(path, schemaFile.Name + ".graphql");
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
    }
}
