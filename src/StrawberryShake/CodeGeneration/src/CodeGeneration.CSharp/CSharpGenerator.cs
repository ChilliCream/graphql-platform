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
using StrawberryShake.CodeGeneration.Analyzers;
using StrawberryShake.CodeGeneration.Analyzers.Models;
using StrawberryShake.CodeGeneration.CSharp.Generators;
using StrawberryShake.CodeGeneration.Descriptors;
using StrawberryShake.CodeGeneration.Mappers;
using StrawberryShake.CodeGeneration.Utilities;
using StrawberryShake.Properties;
using static HotChocolate.Language.Utf8GraphQLParser;
using static StrawberryShake.CodeGeneration.Utilities.DocumentHelper;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public static class CSharpGenerator
    {
        private static readonly ICodeGenerator[] _generators =
        {
            new ClientGenerator(),
            new EntityTypeGenerator(),
            new EntityIdFactoryGenerator(),
            new DependencyInjectionGenerator(),
            new InputValueFormatterGenerator(),
            new EnumGenerator(),
            new EnumParserGenerator(),
            new JsonResultBuilderGenerator(),
            new OperationDocumentGenerator(),
            new OperationServiceGenerator(),
            new ResultDataFactoryGenerator(),
            new ResultFromEntityTypeMapperGenerator(),
            new ResultInfoGenerator(),
            new ResultTypeGenerator(),
            new InputTypeGenerator(),
            new ResultInterfaceGenerator(),
            new DataTypeGenerator()
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
                settings.RequestStrategy);

            // First we run all mappers that do not have any dependencies on others.
            EntityIdFactoryDescriptorMapper.Map(clientModel, context);

            // Second we start with the type descriptor mapper which creates
            // the type structure for the generators.
            // The following mappers can depend on this foundational data.
            TypeDescriptorMapper.Map(clientModel, context);

            // now we execute all mappers that depend on the previous type mappers.
            OperationDescriptorMapper.Map(clientModel, context);
            DependencyInjectionMapper.Map(clientModel, context);
            DataTypeDescriptorMapper.Map(clientModel, context);
            EntityTypeDescriptorMapper.Map(clientModel, context);
            ResultBuilderDescriptorMapper.Map(clientModel, context);

            // Lastly we generate the client mapper
            ClientDescriptorMapper.Map(clientModel, context);

            // Last we execute all our generators with the descriptiptors.
            var code = new StringBuilder();
            var documents = new List<CSharpDocument>();

            foreach (var descriptor in context.GetAllDescriptors())
            {
                foreach (var generator in _generators)
                {
                    if (generator.CanHandle(descriptor))
                    {
                        documents.Add(WriteDocument(generator, descriptor, code));
                    }
                }
            }

            return new(documents);
        }

        private static CSharpDocument WriteDocument(
            ICodeGenerator generator,
            ICodeDescriptor descriptor,
            StringBuilder code)
        {
            code.Clear();

            using var writer = new CodeWriter(code);

#if DEBUG
            writer.WriteLine("// " + generator.GetType().FullName);
            writer.WriteLine();
#endif

            generator.Generate(writer, descriptor, out string fileName);

            writer.Flush();
            return new(fileName, code.ToString());
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
            [NotNullWhen(true)] out ISchema? schema)
        {
            try
            {
                schema = SchemaHelper.Load(files, strictValidation);
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
    }
}
