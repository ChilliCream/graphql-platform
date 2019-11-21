using System.Text;
using System.Security.Cryptography;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using HotChocolate.Language;
using StrawberryShake.Generators;
using IOPath = System.IO.Path;
using HCError = HotChocolate.IError;
using HCErrorBuilder = HotChocolate.ErrorBuilder;

namespace StrawberryShake.Tools
{
    public abstract class CompileCommandBase
        : ICommand
    {
        [Argument(0, "path")]
        public string? Path { get; set; }

        [Option("-s|--SearchForClients")]
        public bool SearchForClients { get; set; }

        public async Task<int> OnExecute()
        {
            try
            {
                if (Path is null || Path == string.Empty)
                {
                    foreach (string clientDirectory in FindDirectories(
                        Environment.CurrentDirectory))
                    {
                        if (!await Compile(clientDirectory))
                        {
                            return 1;
                        }
                    }
                    return 0;
                }
                else if (SearchForClients)
                {
                    foreach (string clientDirectory in FindDirectories(Path))
                    {
                        if (!await Compile(clientDirectory))
                        {
                            return 1;
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

        private IEnumerable<string> FindDirectories(string path)
        {
            foreach (string configFile in Directory.GetFiles(
                path,
                WellKnownFiles.Config,
                SearchOption.AllDirectories))
            {
                string directory = IOPath.GetDirectoryName(configFile)!;
                if (Directory.GetFiles(directory, "*.graphql").Length > 0)
                {
                    yield return directory;
                }
            }
        }

        private async Task<bool> Compile(string path)
        {
            Configuration? config = await Configuration.LoadConfig(path);

            if (config is null)
            {
                return false;
            }

            return await Compile(path, config);
        }

        private async Task<bool> Compile(string path, Configuration config)
        {
            var stopwatch = Stopwatch.StartNew();
            WriteCompileStartedMessage();

            var schemaFiles = new HashSet<string>();
            ClientGenerator generator = ClientGenerator.New();
            generator.SetOutput(IOPath.Combine(path, WellKnownDirectories.Generated));

            if (!string.IsNullOrEmpty(config.ClientName))
            {
                generator.SetClientName(config.ClientName!);
            }

            var errors = new List<HCError>();

            IReadOnlyList<DocumentInfo> documents =
                await LoadGraphQLDocumentsAsync(path, generator, errors);

            if (errors.Count > 0)
            {
                WriteErrors(errors);
                return false;
            }

            bool result = await Compile(path, documents, config, generator);

            WriteCompileCompletedMessage(path, stopwatch);

            return result;
        }

        protected abstract Task<bool> Compile(
            string path,
            IReadOnlyList<DocumentInfo> documents,
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

        private async Task<IReadOnlyList<DocumentInfo>> LoadGraphQLDocumentsAsync(
            string path,
            ClientGenerator generator,
            ICollection<HCError> errors)
        {
            Configuration? configuration = await Configuration.LoadConfig(path);
            if (configuration is null)
            {
                throw new InvalidOperationException(
                    "The configuration does not exist.");
            }

            if (configuration.Schemas is null)
            {
                throw new InvalidOperationException(
                    "The configuration has no schemas defined.");
            }

            Dictionary<string, SchemaFile> schemas =
                configuration.Schemas.Where(t => t.Name != null)
                    .ToDictionary(t => t.Name!);

            IReadOnlyList<DocumentInfo> files = await GetGraphQLFiles(path, errors);

            foreach (DocumentInfo document in files)
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

                    if (schemas.TryGetValue(
                        IOPath.GetFileName(document.FileName),
                        out SchemaFile? file))
                    {
                        name = file.Name!;
                    }

                    generator.AddSchemaDocument(
                        name,
                        document.Document);
                }
            }

            return files;
        }

        private async Task<IReadOnlyList<DocumentInfo>> GetGraphQLFiles(
            string path, ICollection<HCError> errors)
        {
            var documents = new List<DocumentInfo>();

            var md5 = MD5.Create();

            foreach (string file in Directory.GetFiles(path, "*.graphql"))
            {
                byte[] buffer = await File.ReadAllBytesAsync(file);

                try
                {
                    DocumentNode document = Utf8GraphQLParser.Parse(buffer);

                    if (document.Definitions.Count > 0)
                    {
                        DocumentKind kind =
                            document.Definitions.Any(t =>
                                t is ITypeSystemDefinitionNode
                                || t is ITypeSystemExtensionNode)
                                ? DocumentKind.Schema
                                : DocumentKind.Query;

                        string text = kind == DocumentKind.Query
                            ? QuerySyntaxSerializer.Serialize(document, false)
                            : SchemaSyntaxSerializer.Serialize(document, false);

                        byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(text));

                        documents.Add(new DocumentInfo
                        (
                            file,
                            kind,
                            document,
                            Convert.ToBase64String(hash)
                        ));
                    }
                }
                catch (SyntaxException ex)
                {
                    HCError error = HCErrorBuilder.New()
                        .SetMessage(ex.Message)
                        .AddLocation(ex.Line, ex.Column)
                        .SetCode("SYNTAX_ERROR")
                        .SetExtension("fileName", file)
                        .Build();

                    errors.Add(error);
                    return Array.Empty<DocumentInfo>();
                }
                catch (NotSupportedException ex)
                {
                    HCError error = HCErrorBuilder.New()
                        .SetMessage(
                            "The filed contained schema definitions and query definitions.")
                        .SetCode("MIXED_DOCUMENTS")
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

        protected struct DocumentInfo
        {
            public DocumentInfo(
                string fileName,
                DocumentKind kind,
                DocumentNode document,
                string hash)
            {
                FileName = fileName;
                Kind = kind;
                Document = document;
                Hash = hash;
            }

            public string FileName { get; }
            public DocumentKind Kind { get; }
            public DocumentNode Document { get; }
            public string Hash { get; }
        }

        protected enum DocumentKind
        {
            Schema,
            Query
        }
    }
}
