using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Validation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using StrawberryShake.CodeGeneration.Analyzers;
using StrawberryShake.CodeGeneration.Analyzers.Models;
using StrawberryShake.CodeGeneration.CSharp.Generators;
using StrawberryShake.CodeGeneration.Mappers;
using StrawberryShake.CodeGeneration.Utilities;
using StrawberryShake.Properties;
using StrawberryShake.Tools.Configuration;
using static HotChocolate.Language.Utf8GraphQLParser;
using static StrawberryShake.CodeGeneration.Utilities.DocumentHelper;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public static class CSharpGenerator
    {
        private static readonly ICSharpSyntaxGenerator[] _generators =
        {
            new ClientGenerator(),
            new ClientInterfaceGenerator(),
            new EntityTypeGenerator(),
            new EntityIdFactoryGenerator(),
            new DependencyInjectionGenerator(),
            new TransportProfileEnumGenerator(),
            new InputValueFormatterGenerator(),
            new EnumGenerator(),
            new EnumParserGenerator(),
            new JsonResultBuilderGenerator(),
            new OperationDocumentGenerator(),
            new OperationServiceGenerator(),
            new OperationServiceInterfaceGenerator(),
            new ResultDataFactoryGenerator(),
            new ResultFromEntityTypeMapperGenerator(),
            new ResultInfoGenerator(),
            new ResultTypeGenerator(),
            new StoreAccessorGenerator(),
            new NoStoreAccessorGenerator(),
            new InputTypeGenerator(),
            new InputTypeStateInterfaceGenerator(),
            new ResultInterfaceGenerator(),
            new DataTypeGenerator(),
            new RazorQueryGenerator()
        };

        public static CSharpGeneratorResult Generate(
            IEnumerable<string> fileNames,
            CSharpGeneratorSettings? settings = null)
        {
            if (fileNames is null)
            {
                throw new ArgumentNullException(nameof(fileNames));
            }

            settings ??= new();

            if (string.IsNullOrEmpty(settings.ClientName))
            {
                throw new ArgumentException(
                    string.Format(
                        Resources.CSharpGenerator_Generate_ArgumentCannotBeNull,
                        nameof(settings.ClientName)),
                    nameof(settings));
            }

            if (string.IsNullOrEmpty(settings.Namespace))
            {
                throw new ArgumentException(
                    string.Format(
                        Resources.CSharpGenerator_Generate_ArgumentCannotBeNull,
                        nameof(settings.Namespace)),
                    nameof(settings));
            }

            var files = new List<GraphQLFile>();
            var errors = new List<IError>();

            // parse the GraphQL files ...
            if (!TryParseDocuments(fileNames, files, errors))
            {
                return new(errors);
            }

            // divide documents into type system document for the schema
            // and executable documents.
            IReadOnlyList<GraphQLFile> typeSystemFiles = files.GetTypeSystemDocuments();
            IReadOnlyList<GraphQLFile> executableFiles = files.GetExecutableDocuments();

            if (typeSystemFiles.Count == 0 || executableFiles.Count == 0)
            {
                // if we do not have any documents we will just return without any errors.
                return new();
            }

            // Since form this point on we will work on a merged executable document we need to
            // index the syntax nodes so that we can link errors to the correct files.
            var fileLookup = new Dictionary<ISyntaxNode, string>();
            IndexSyntaxNodes(files, fileLookup);

            // We try true create a schema from the type system documents.
            // If we cannot create a schema we will return the schema validation errors.
            if (!TryCreateSchema(
                typeSystemFiles,
                fileLookup,
                errors,
                settings.StrictSchemaValidation,
                settings.NoStore,
                out ISchema? schema))
            {
                return new(errors);
            }

            // Next we will start validating the executable documents.
            if (!TryValidateRequest(schema, executableFiles, fileLookup, errors))
            {
                return new(errors);
            }

            // At this point we have a valid schema and know that our documents are executable
            // against the schema.
            //
            // In order to generate the client code we will first need to create a client model
            // which represents the logical parts of the executable documents.
            var analyzer = new DocumentAnalyzer();
            analyzer.SetSchema(schema);

            foreach (GraphQLFile executableDocument in executableFiles)
            {
                analyzer.AddDocument(executableDocument.Document);
            }

            ClientModel clientModel = analyzer.Analyze();

            // With the client model we finally can create CSharp code.
            return Generate(clientModel, settings);
        }

        public static CSharpGeneratorResult Generate(
            ClientModel clientModel,
            CSharpGeneratorSettings settings)
        {
            if (clientModel is null)
            {
                throw new ArgumentNullException(nameof(clientModel));
            }

            if (string.IsNullOrEmpty(settings.ClientName))
            {
                throw new ArgumentException(
                    string.Format(
                        Resources.CSharpGenerator_Generate_ArgumentCannotBeNull,
                        nameof(settings.ClientName)),
                    nameof(settings));
            }

            if (string.IsNullOrEmpty(settings.Namespace))
            {
                throw new ArgumentException(
                    string.Format(
                        Resources.CSharpGenerator_Generate_ArgumentCannotBeNull,
                        nameof(settings.Namespace)),
                    nameof(settings));
            }

            var context = new MapperContext(
                settings.Namespace,
                settings.ClientName,
                settings.HashProvider,
                settings.RequestStrategy,
                settings.TransportProfiles);

            // First we run all mappers that do not have any dependencies on others.
            EntityIdFactoryDescriptorMapper.Map(clientModel, context);

            // Second we start with the type descriptor mapper which creates
            // the type structure for the generators.
            // The following mappers can depend on this foundational data.
            TypeDescriptorMapper.Map(clientModel, context);

            // now we execute all mappers that depend on the previous type mappers.
            OperationDescriptorMapper.Map(clientModel, context);
            StoreAccessorMapper.Map(clientModel, context);
            DataTypeDescriptorMapper.Map(clientModel, context);
            EntityTypeDescriptorMapper.Map(clientModel, context);
            ResultBuilderDescriptorMapper.Map(clientModel, context);

            // We generate the client mapper next as we have all components of the client generated
            ClientDescriptorMapper.Map(clientModel, context);

            // Lastly we generate the DI code, as we now have collected everything
            DependencyInjectionMapper.Map(clientModel, context);

            // Last we execute all our generators with the descriptors.
            IReadOnlyList<GeneratorResult> results = GenerateCSharpDocuments(context, settings);

            var documents = new List<SourceDocument>();

            if (settings.SingleCodeFile)
            {
                GenerateSingleCSharpDocument(
                    results.Where(t => t.Result.IsCSharpDocument),
                    SourceDocumentKind.CSharp,
                    settings.ClientName,
                    documents);

                if (results.Any(t => t.Result.IsRazorComponent))
                {
                    GenerateSingleCSharpDocument(
                        results.Where(t => t.Result.IsRazorComponent),
                        SourceDocumentKind.Razor,
                        settings.ClientName,
                        documents);
                }
            }
            else
            {
                GenerateMultipleCSharpDocuments(
                    results.Where(t => t.Result.IsCSharpDocument),
                    SourceDocumentKind.CSharp,
                    documents);

                if (results.Any(t => t.Result.IsRazorComponent))
                {
                    GenerateMultipleCSharpDocuments(
                        results.Where(t => t.Result.IsRazorComponent),
                        SourceDocumentKind.Razor,
                        documents);
                }
            }

            // If persisted queries is enabled we will add the queries as documents.
            if (settings.RequestStrategy == RequestStrategy.PersistedQuery)
            {
                foreach (var operation in context.Operations)
                {
                    documents.Add(new SourceDocument(
                        operation.Name,
                        Encoding.UTF8.GetString(operation.Body),
                        SourceDocumentKind.GraphQL,
                        operation.HashValue));
                }
            }

            return new(
                documents,
                clientModel.Operations
                    .Select(t => t.OperationType)
                    .Distinct()
                    .ToArray());
        }

        private static void GenerateSingleCSharpDocument(
            IEnumerable<GeneratorResult> results,
            SourceDocumentKind kind,
            string fileName,
            ICollection<SourceDocument> documents)
        {
            var code = new StringBuilder();

            // marker for style cop to ignore this code
            code.AppendLine("// <auto-generated/>");

            // enable nullability settings
            code.AppendLine("#nullable enable");

            CompilationUnitSyntax compilationUnit = CompilationUnit();

            foreach (var group in results.GroupBy(t => t.Result.Namespace).OrderBy(t => t.Key))
            {
                NamespaceDeclarationSyntax namespaceDeclaration =
                    NamespaceDeclaration(IdentifierName(group.Key));

                foreach (var item in group)
                {
                    BaseTypeDeclarationSyntax typeDeclaration = item.Result.TypeDeclaration;
#if DEBUG
                    SyntaxTriviaList trivia = typeDeclaration
                        .GetLeadingTrivia()
                        .Insert(0, Comment("// " + item.Generator.FullName));

                    typeDeclaration = typeDeclaration.WithLeadingTrivia(trivia);
#endif
                    namespaceDeclaration = namespaceDeclaration.AddMembers(typeDeclaration);
                }

                compilationUnit = compilationUnit.AddMembers(namespaceDeclaration);
            }

            compilationUnit = compilationUnit.NormalizeWhitespace(elasticTrivia: true);

            code.AppendLine();
            code.AppendLine(compilationUnit.ToFullString());

            documents.Add(new(
                fileName,
                code.ToString(),
                kind));
        }

        private static IReadOnlyList<GeneratorResult> GenerateCSharpDocuments(
            MapperContext context,
            CSharpGeneratorSettings settings)
        {
            var generatorSettings = new CSharpSyntaxGeneratorSettings(
                settings.NoStore,
                settings.InputRecords,
                settings.EntityRecords,
                settings.RazorComponents);

            var results = new List<GeneratorResult>();

            foreach (var descriptor in context.GetAllDescriptors())
            {
                foreach (var generator in _generators)
                {
                    if (generator.CanHandle(descriptor, generatorSettings))
                    {
                        CSharpSyntaxGeneratorResult result =
                            generator.Generate(descriptor, generatorSettings);
                        results.Add(new(generator.GetType(), result));
                    }
                }
            }

            return results;
        }

        private static void GenerateMultipleCSharpDocuments(
            IEnumerable<GeneratorResult> results,
            SourceDocumentKind kind,
            ICollection<SourceDocument> documents)
        {
            foreach (var group in results.GroupBy(t => t.Result.Namespace).OrderBy(t => t.Key))
            {
                foreach (var item in group)
                {
                    BaseTypeDeclarationSyntax typeDeclaration = item.Result.TypeDeclaration;
#if DEBUG
                    SyntaxTriviaList trivia = typeDeclaration
                        .GetLeadingTrivia()
                        .Insert(0, Comment("// " + item.Generator.FullName));

                    typeDeclaration = typeDeclaration.WithLeadingTrivia(trivia);
#endif
                    CompilationUnitSyntax compilationUnit =
                        CompilationUnit().AddMembers(
                            NamespaceDeclaration(IdentifierName(group.Key)).AddMembers(
                                typeDeclaration));

                    compilationUnit = compilationUnit.NormalizeWhitespace(elasticTrivia: true);

                    var code = new StringBuilder();

                    // marker for style cop to ignore this code
                    code.AppendLine("// <auto-generated/>");

                    // enable nullability settings
                    code.AppendLine("#nullable enable");

                    code.AppendLine();
                    code.AppendLine(compilationUnit.ToFullString());

                    documents.Add(new(
                        item.Result.FileName,
                        code.ToString(),
                        kind,
                        path: item.Result.Path));
                }
            }
        }

        private static bool TryParseDocuments(
            IEnumerable<string> fileNames,
            ICollection<GraphQLFile> files,
            ICollection<IError> errors)
        {
            foreach (var fileName in fileNames)
            {
                try
                {
                    DocumentNode document = Parse(File.ReadAllBytes(fileName));
                    files.Add(new(fileName, document));
                }
                catch (SyntaxException syntaxException)
                {
                    errors.Add(syntaxException.SyntaxError(fileName));
                }
            }

            return errors.Count == 0;
        }

        private static bool TryCreateSchema(
            IReadOnlyList<GraphQLFile> files,
            Dictionary<ISyntaxNode, string> fileLookup,
            ICollection<IError> errors,
            bool strictValidation,
            bool noStore,
            [NotNullWhen(true)] out ISchema? schema)
        {
            try
            {
                schema = SchemaHelper.Load(files, strictValidation, noStore);
                return true;
            }
            catch (SchemaException ex)
            {
                foreach (ISchemaError error in ex.Errors)
                {
                    errors.Add(error.SchemaError(fileLookup));
                }

                schema = null;
                return false;
            }
        }

        private static bool TryValidateRequest(
            ISchema schema,
            IReadOnlyList<GraphQLFile> executableFiles,
            Dictionary<ISyntaxNode, string> fileLookup,
            List<IError> errors)
        {
            IDocumentValidator validator = CreateDocumentValidator();

            DocumentNode document = MergeDocuments(executableFiles);
            DocumentValidatorResult validationResult = validator.Validate(schema, document);

            if (validationResult.HasErrors)
            {
                errors.AddRange(
                    validationResult.Errors.Select(
                        error => error.WithFileReference(fileLookup)));
                return false;
            }

            return true;
        }

        private static IDocumentValidator CreateDocumentValidator() =>
            new ServiceCollection()
                .AddValidation()
                .Services
                .BuildServiceProvider()
                .GetRequiredService<IDocumentValidatorFactory>()
                .CreateValidator();

        private static DocumentNode MergeDocuments(IReadOnlyList<GraphQLFile> executableFiles) =>
            new(executableFiles.SelectMany(t => t.Document.Definitions).ToList());

        private class GeneratorResult
        {
            public GeneratorResult(Type generator, CSharpSyntaxGeneratorResult result)
            {
                Generator = generator;
                Result = result;
            }

            public Type Generator { get; }

            public CSharpSyntaxGeneratorResult Result { get; }
        }
    }
}
