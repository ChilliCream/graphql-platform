using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Validation;
using StrawberryShake.CodeGeneration.Analyzers;
using StrawberryShake.CodeGeneration.Analyzers.Models;
using StrawberryShake.CodeGeneration.Utilities;
using SyntaxVisitor = HotChocolate.Language.Visitors.SyntaxVisitor;
using static StrawberryShake.CodeGeneration.CodeGenerationThrowHelper;
using HotChocolate.Language.Visitors;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class CSharpGenerator
    {
        public CSharpGeneratorResult Generate(
            IEnumerable<string> graphQLFiles,
            string clientName = "GraphQLClient",
            string @namespace = "StrawberryShake.GraphQL")
        {
            if (graphQLFiles is null)
            {
                throw new ArgumentNullException(nameof(graphQLFiles));
            }

            var errors = new List<IError>();
            var documents = new List<(string file, DocumentNode document)>();

            foreach (var file in graphQLFiles)
            {
                try
                {
                    documents.Add((file, Utf8GraphQLParser.Parse(File.ReadAllBytes(file))));
                }
                catch (SyntaxException syntaxException)
                {
                    errors.Add(
                        Generator_SyntaxException(
                            syntaxException,
                            file));
                }
            }

            if (errors.Count > 0)
            {
                return new CSharpGeneratorResult(
                    new List<CSharpDocument>(),
                    errors);
            }

            var typeSystemDocs = documents.GetTypeSystemDocuments();
            var executableDocs = documents.GetExecutableDocuments();

            if (typeSystemDocs.Count == 0)
            {
                errors.AddRange(Generator_NoTypeDocumentsFound());
            }

            if (executableDocs.Count == 0)
            {
                errors.AddRange(Generator_NoExecutableDocumentsFound());
            }

            if (errors.Any())
            {
                return new CSharpGeneratorResult(
                    new List<CSharpDocument>(),
                    errors);
            }

            ISchema schema = SchemaHelper.Load(typeSystemDocs);

            IDocumentValidator validator = new ServiceCollection()
                .AddValidation()
                .Services
                .BuildServiceProvider()
                .GetRequiredService<IDocumentValidatorFactory>()
                .CreateValidator();

            var definitions = executableDocs.SelectMany(t => t.document.Definitions).ToList();
            var merged = new DocumentNode(definitions);
            var validationResult = validator.Validate(schema, merged);
            var lookup = new Dictionary<ISyntaxNode, string>();

            foreach (var doc in executableDocs)
            {
                IndexSyntaxNodes(doc.document, doc.file, lookup);
            }

            if (validationResult.HasErrors)
            {
                errors.AddRange(
                    validationResult.Errors.Select(
                        error =>
                        {
                            var extensions = new Dictionary<string, object?>
                            {
                                { TitleExtensionKey, "Schema validation error" }
                            };

                            // if the error has a syntax node we will try to lookup the
                            // document and add the filename to the error.
                            if (error is Error { SyntaxNode: { } node } &&
                                lookup.TryGetValue(node, out var filename))
                            {
                                extensions.Add(FileExtensionKey, filename);
                            }

                            return error
                                .WithCode(CodeGenerationErrorCodes.SchemaValidationError)
                                .WithExtensions(extensions);
                        }));
            }

            if (errors.Any())
            {
                return new CSharpGeneratorResult(
                    new List<CSharpDocument>(),
                    errors);
            }

            var analyzer = new DocumentAnalyzer();
            analyzer.SetSchema(schema);

            foreach ((string file, DocumentNode document) executableDocument in executableDocs)
            {
                analyzer.AddDocument(executableDocument.document);
            }

            ClientModel clientModel = analyzer.Analyze();

            var executor = new CSharpGeneratorExecutor();

            return new CSharpGeneratorResult(
                executor.Generate(
                    clientModel,
                    @namespace,
                    clientName).ToList(),
                errors);
        }

        private void IndexSyntaxNodes(
            DocumentNode document,
            string filename,
            Dictionary<ISyntaxNode, string> lookup)
        {
            SyntaxVisitor.Create(
                enter: node =>
                {
                    lookup.Add(node, filename);
                    return SyntaxVisitor.Continue;
                },
                defaultAction: SyntaxVisitor.Continue,
                options: new SyntaxVisitorOptions
                {
                    VisitArguments = true,
                    VisitDescriptions = true,
                    VisitDirectives = true,
                    VisitNames = true
                });
        }
    }
}
