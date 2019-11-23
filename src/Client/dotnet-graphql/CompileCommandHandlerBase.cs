using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using HotChocolate.Language;
using StrawberryShake.Generators;
using HCError = HotChocolate.IError;
using HCErrorBuilder = HotChocolate.ErrorBuilder;

namespace StrawberryShake.Tools
{
    public abstract class CompileCommandHandlerBase<TArg, TCtx>
        : CommandHandler<TArg>
        where TCtx : ICompileContext
    {
        protected CompileCommandHandlerBase(
            IFileSystem fileSystem,
            IHttpClientFactory httpClientFactory,
            IConfigurationStore configurationStore,
            IConsoleOutput output)
        {
            FileSystem = fileSystem;
            HttpClientFactory = httpClientFactory;
            ConfigurationStore = configurationStore;
            Output = output;
        }

        public IFileSystem FileSystem { get; }

        public IHttpClientFactory HttpClientFactory { get; }

        public IConfigurationStore ConfigurationStore { get; }

        public IConsoleOutput Output { get; }

        protected virtual string ActivityName => "Compile";

        public override Task<int> ExecuteAsync(
            TArg arguments,
            CancellationToken cancellationToken)
        {
            TCtx context = CreateContext(arguments);
            return ExecuteAsync(context, cancellationToken);
        }

        protected abstract TCtx CreateContext(TArg arguments);

        protected abstract Task<bool> Compile(
            TCtx context,
            string path,
            Configuration config,
            ClientGenerator generator,
            IReadOnlyList<DocumentInfo> documents,
            ICollection<HCError> errors);

        private async Task<int> ExecuteAsync(
            TCtx context,
            CancellationToken cancellationToken)
        {
            string path = FileSystem.ResolvePath(context.Path);

            if (context.Search)
            {
                int errorCode = 0;

                foreach (string clientDirectory in FileSystem.GetClientDirectories(path))
                {
                    if (!await Compile(context, clientDirectory))
                    {
                        errorCode = 1;
                    }
                }

                return errorCode;
            }
            else
            {
                return (await Compile(context, path)) ? 0 : 1;
            }
        }

        private async Task<bool> Compile(TCtx context, string path)
        {
            Configuration? config = await ConfigurationStore.TryLoadAsync(path);

            if (config is { })
            {
                return await Compile(context, path, config);
            }

            return false;
        }

        private async Task<bool> Compile(TCtx context, string path, Configuration configuration)
        {
            using IActivity activity = Output.WriteActivity(ActivityName);
            try
            {
                var schemaFiles = new HashSet<string>();
                ClientGenerator generator = ClientGenerator.New();
                generator.SetOutput(FileSystem.CombinePath(
                    path, WellKnownDirectories.Generated));
                generator.SetClientName(configuration.ClientName);

                var documents = new List<DocumentInfo>();
                var errors = new List<HCError>();
                await LoadGraphQLDocumentsAsync(
                    path, configuration, generator,
                    documents, errors);

                if (errors.Count > 0)
                {
                    activity.WriteErrors(errors);
                    return false;
                }

                bool success = await Compile(
                    context, path, configuration, generator,
                    documents, errors);

                if (errors.Count > 0)
                {
                    activity.WriteErrors(errors);
                    return false;
                }

                return success;
            }
            catch (GeneratorException ex)
            {
                activity.WriteErrors(ex.Errors);
                return false;
            }
        }

        private async Task LoadGraphQLDocumentsAsync(
            string path,
            Configuration configuration,
            ClientGenerator generator,
            ICollection<DocumentInfo> documents,
            ICollection<HCError> errors)
        {
            Dictionary<string, SchemaFile> schemas =
                configuration.Schemas.Where(t => t.Name != null)
                    .ToDictionary(t => t.Name!);

            await LoadGraphQLFiles(path, documents, errors);

            foreach (DocumentInfo document in documents)
            {
                if (document.Kind == DocumentKind.Query)
                {
                    generator.AddQueryDocument(
                        FileSystem.GetFileNameWithoutExtension(document.FileName),
                        document.FileName,
                        document.Document);
                }
                else
                {
                    string name = FileSystem.GetFileNameWithoutExtension(document.FileName);

                    if (schemas.TryGetValue(
                        FileSystem.GetFileName(document.FileName),
                        out SchemaFile? file))
                    {
                        name = file.Name;
                    }

                    generator.AddSchemaDocument(
                        name,
                        document.Document);
                }
            }
        }

        private async Task LoadGraphQLFiles(
            string path,
            ICollection<DocumentInfo> documents,
            ICollection<HCError> errors)
        {
            var md5 = MD5.Create();

            foreach (string file in FileSystem.GetGraphQLFiles(path))
            {

                byte[] buffer = await FileSystem.ReadAllBytesAsync(file);

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
                    break;
                }
                catch (NotSupportedException)
                {
                    HCError error = HCErrorBuilder.New()
                        .SetMessage(
                            "The filed contained schema definitions and query definitions.")
                        .SetCode("MIXED_DOCUMENTS")
                        .SetExtension("fileName", file)
                        .Build();

                    errors.Add(error);
                    break;
                }
            }
        }

        protected readonly struct DocumentInfo
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
