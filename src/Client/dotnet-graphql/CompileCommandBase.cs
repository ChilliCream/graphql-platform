using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using HotChocolate;
using HotChocolate.Language;
using StrawberryShake.Generators;
using IOPath = System.IO.Path;
using HCError = HotChocolate.IError;

namespace StrawberryShake.Tools
{
    public abstract class CompileCommandBase
        : ICommand
    {
        [Argument(0, "path")]
        public string Path { get; set; }

        public async Task<int> OnExecute()
        {
            try
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
                            try
                            {
                                Configuration config = await LoadConfig(directory);
                                if (config.Schemas.Count > 0)
                                {
                                    if (!(await Compile(directory, config)))
                                    {
                                        return 1;
                                    }
                                }
                            }
                            catch
                            {
                                // ignore invalid configs
                            }
                        }
                    }
                    return 0;
                }
                else
                {
                    return (await Compile(Path)) ? 0 : 1;
                }
            }
            catch (GeneratorException ex)
            {
                WriteErrors(ex.Errors);
                return 1;
            }
        }


        private async Task<bool> Compile(string path)
        {
            Configuration config = await LoadConfig(path);
            return await Compile(path, config); ;
        }

        private async Task<bool> Compile(string path, Configuration config)
        {
            var stopwatch = Stopwatch.StartNew();
            WriteCompileStartedMessage();

            var schemaFiles = new HashSet<string>();
            ClientGenerator generator = ClientGenerator.New();
            generator.SetOutput(IOPath.Combine(path, "Generated"));

            if (!string.IsNullOrEmpty(config.ClientName))
            {
                generator.SetClientName(config.ClientName);
            }

            var errors = new List<HCError>();
            await LoadGraphQLDocumentsAsync(path, generator, errors);
            if (errors.Count > 0)
            {
                WriteErrors(errors);
                return false;
            }

            bool result = await Compile(path, config, generator);

            WriteCompileCompletedMessage(path, stopwatch);

            return result;
        }

        protected abstract Task<bool> Compile(
            string path,
            Configuration config,
            ClientGenerator generator);

        protected virtual void WriteCompileStartedMessage()
        {
            Console.WriteLine("Compile started.");
        }

        protected virtual void WriteCompileCompletedMessage(
            string path, Stopwatch stopwatch)
        {
            Console.WriteLine(
                $"Compile completed in {stopwatch.ElapsedMilliseconds} ms " +
                $"for {path}.");
        }

        protected async Task<Configuration> LoadConfig(string path)
        {
            Configuration config;

            using (var stream = File.OpenRead(IOPath.Combine(path, "config.json")))
            {
                config = await JsonSerializer.DeserializeAsync<Configuration>(
                    stream,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                    });
            }

            return config;
        }

        private async Task LoadGraphQLDocumentsAsync(
            string path,
            ClientGenerator generator,
            ICollection<HCError> errors)
        {
            Dictionary<string, SchemaFile> schemaConfigs =
                (await LoadConfig(path)).Schemas.ToDictionary(t => t.Name);

            foreach (DocumentInfo document in await GetGraphQLFiles(path, errors))
            {
                if (document.Kind == DocumentKind.Query)
                {
                    generator.AddQueryDocument(
                        IOPath.GetFileNameWithoutExtension(document.FileName),
                        document.FileName,
                        document.Document);
                }
                else
                {
                    string name = IOPath.GetFileNameWithoutExtension(
                        document.FileName);

                    if (schemaConfigs.TryGetValue(
                        IOPath.GetFileName(document.FileName),
                        out SchemaFile file))
                    {
                        name = file.Name;
                    }

                    generator.AddSchemaDocument(
                        name,
                        document.Document);
                }
            }
        }

        private async Task<IReadOnlyList<DocumentInfo>> GetGraphQLFiles(
            string path, ICollection<HCError> errors)
        {
            var documents = new List<DocumentInfo>();

            foreach (string file in Directory.GetFiles(path, "*.graphql"))
            {
                byte[] buffer = await File.ReadAllBytesAsync(file);

                try
                {
                    DocumentNode document = Utf8GraphQLParser.Parse(buffer);

                    if (document.Definitions.Count > 0)
                    {
                        DocumentKind kind =
                            document.Definitions.Any(t => t is ITypeSystemDefinitionNode)
                                ? DocumentKind.Schema
                                : DocumentKind.Query;

                        documents.Add(new DocumentInfo
                        {
                            Kind = kind,
                            FileName = file,
                            Document = document
                        });
                    }
                }
                catch (SyntaxException ex)
                {
                    HCError error = ErrorBuilder.New()
                        .SetMessage(ex.Message)
                        .AddLocation(new HotChocolate.Location(ex.Line, ex.Column))
                        .SetCode("SYNTAX_ERROR")
                        .SetExtension("fileName", file)
                        .Build();

                    errors.Add(error);
                    return Array.Empty<DocumentInfo>();
                }
            }

            return documents;
        }

        protected static void WriteErrors(IReadOnlyList<HCError> errors)
        {
            if (errors.Count > 0)
            {
                foreach (HCError error in errors)
                {
                    HotChocolate.Location location = error.Locations[0];
                    string code = error.Code ?? "GQL";
                    Console.WriteLine(
                        $"{IOPath.GetFullPath((string)error.Extensions["fileName"])}" +
                        $"({location.Line},{location.Column}): " +
                        $"error {code}: {error.Message}");
                }
            }
        }

        private struct DocumentInfo
        {
            public string FileName { get; set; }
            public DocumentKind Kind { get; set; }
            public DocumentNode Document { get; set; }
        }

        private enum DocumentKind
        {
            Schema,
            Query
        }
    }
}
