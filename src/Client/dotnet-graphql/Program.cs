using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using HotChocolate.Language;
using StrawberryShake.Generators;
using IOPath = System.IO.Path;
using HCError = HotChocolate.IError;
using static McMaster.Extensions.CommandLineUtils.CommandLineApplication;
using HotChocolate;

namespace dotnet_graphql
{
    internal class Program
    {
        internal static Task<int> Main(string[] args) =>
            ExecuteAsync<CommandRouter>(args);
    }

    [Command(ThrowOnUnexpectedArgument = false)]
    public class CommandRouter
        : ICommand
    {
        [Argument(0)]
        public Command Command { get; set; }

        public string[] RemainingArgs { get; set; }

        public Task<int> OnExecute()
        {
            switch (Command)
            {
                case Command.Compile:
                    return ExecuteAsync<CompileCommand>(RemainingArgs);

                default:
                    return Task.FromResult(1);
            }
        }
    }


    public interface ICommand
    {
        Task<int> OnExecute();
    }


    public enum Command
    {
        Init,
        Compile
    }

    public class CompileCommand
        : ICommand
    {
        [Argument(0)]
        public string Path { get; set; }

        public async Task<int> OnExecute()
        {
            if (Path is null)
            {
                Path = Environment.CurrentDirectory;
            }

            Configuration config = await LoadConfig();

            var schemaFiles = new HashSet<string>();
            ClientGenerator generator = ClientGenerator.New();
            generator.SetOutput(IOPath.Combine(Path, "Generated"));

            if (!string.IsNullOrEmpty(config.ClientName))
            {
                generator.SetClientName(config.ClientName);
            }

            try
            {
                await LoadGraphQLDocumentsAsync(generator);
            }
            catch (SyntaxException ex)
            {
                HCError error = ErrorBuilder.New()
                    .SetMessage(ex.Message)
                    .AddLocation(new HotChocolate.Location(ex.Line, ex.Column))
                    .SetCode("SYNTAX_ERROR")
                    .Build();

                WriteErrors(new[] { error });
                return 1;
            }

            IReadOnlyList<HCError> validationErrors = generator.Validate();
            if (validationErrors.Count > 0)
            {
                WriteErrors(validationErrors);
                return 1;
            }

            await generator.BuildAsync();
            return 0;
        }

        private async Task<Configuration> LoadConfig()
        {
            Configuration config;

            using (var stream = File.OpenRead(IOPath.Combine(Path, "config.json")))
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

        private async Task LoadGraphQLDocumentsAsync(ClientGenerator generator)
        {
            Dictionary<string, SchemaFile> schemaConfigs =
                (await LoadConfig()).Schemas.ToDictionary(t => t.Name);

            foreach (DocumentInfo document in await GetGraphQLFiles())
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

        private async Task<IReadOnlyList<DocumentInfo>> GetGraphQLFiles()
        {
            var documents = new List<DocumentInfo>();

            foreach (string file in Directory.GetFiles(Path, "*.graphql"))
            {
                byte[] buffer = await File.ReadAllBytesAsync(file);
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

            return documents;
        }

        private static void WriteErrors(IReadOnlyList<HCError> errors)
        {
            if (errors.Count > 0)
            {
                foreach (HCError error in errors)
                {
                    HotChocolate.Location location = error.Locations[0];
                    string code = error.Code ?? "GQL";
                    Console.WriteLine(
                        $"{error.Extensions["fileName"]}" +
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

    public class Configuration
    {
        public List<SchemaFile> Schemas { get; set; }
        public string ClientName { get; set; }
    }

    public class SchemaFile
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string File { get; set; }
    }
}
