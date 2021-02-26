using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using ChilliCream.Testing;
using HotChocolate;
using HotChocolate.Language;
using Snapshooter.Xunit;
using StrawberryShake.CodeGeneration.Analyzers;
using StrawberryShake.CodeGeneration.Analyzers.Models;
using StrawberryShake.CodeGeneration.Utilities;
using Xunit;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public static class GeneratorTestHelper
    {
        public static IReadOnlyList<IError> AssertError(params string[] sourceTexts)
        {
            ClientModel clientModel = CreateClientModel(sourceTexts);

            var documents = new StringBuilder();
            var documentNames = new HashSet<string>();

            var result = CSharpGenerator.Generate(clientModel, "Foo", "FooClient");

            Assert.True(
                result.Errors.Any(), 
                "It is expected that the result has no generator errors!");

            return result.Errors;
        }

        public static void AssertResult(params string[] sourceTexts)
        {
            ClientModel clientModel = CreateClientModel(sourceTexts);

            var documents = new StringBuilder();
            var documentNames = new HashSet<string>();

            documents.AppendLine("// ReSharper disable BuiltInTypeReferenceStyle");
            documents.AppendLine("// ReSharper disable RedundantNameQualifier");
            documents.AppendLine("// ReSharper disable ArrangeObjectCreationWhenTypeEvident");
            documents.AppendLine("// ReSharper disable UnusedType.Global");
            documents.AppendLine("// ReSharper disable PartialTypeWithSinglePart");
            documents.AppendLine("// ReSharper disable UnusedMethodReturnValue.Local");
            documents.AppendLine("// ReSharper disable ConvertToAutoProperty");
            documents.AppendLine("// ReSharper disable UnusedMember.Global");
            documents.AppendLine("// ReSharper disable SuggestVarOrType_SimpleTypes");
            documents.AppendLine("// ReSharper disable InconsistentNaming");
            documents.AppendLine();

            var result = CSharpGenerator.Generate(clientModel, "Foo", "FooClient");

            Assert.False(
                result.Errors.Any(), 
                "It is expected that the result has no generator errors!");

            foreach (CSharpDocument document in result.CSharpDocuments)
            {
                if (!documentNames.Add(document.Name))
                {
                    Assert.True(false, $"Document name duplicated {document.Name}");
                }

                documents.AppendLine("// " + document.Name);
                documents.AppendLine();
                documents.AppendLine(document.SourceText);
                documents.AppendLine();
            }

            documents.ToString().MatchSnapshot();

            IReadOnlyList<Diagnostic> diagnostics =
                CSharpCompiler.GetDiagnosticErrors(documents.ToString());

            if (diagnostics.Any())
            {
                Assert.True(false,
                    "Diagnostic Errors: \n" +
                    diagnostics
                        .Select(x =>
                            $"{x.GetMessage()}" +
                            $" (Line: {x.Location.GetLineSpan().StartLinePosition.Line})")
                        .Aggregate((acc, val) => acc + "\n" + val));
            }
        }

        public static void AssertStarWarsResult(params string[] sourceTexts)
        {
            var source = new string[sourceTexts.Length];

            source[0] = FileResource.Open("Schema.graphql");
            source[1] = FileResource.Open("Schema.extensions.graphql");
            
            Array.Copy(
                sourceTexts, 
                sourceIndex: 0, 
                source, 
                destinationIndex: 2, 
                length: sourceTexts.Length);

            AssertResult(source);
        }

        private static ClientModel CreateClientModel(params string[] sourceText)
        {
            var files = sourceText
                .Select(sourceText => new GraphQLFile(Utf8GraphQLParser.Parse(sourceText)))
                .ToList();

            var typeSystemDocs = files.GetTypeSystemDocuments().ToList();
            var executableDocs = files.GetExecutableDocuments().ToList();

            var analyzer = new DocumentAnalyzer();

            analyzer.SetSchema(SchemaHelper.Load(typeSystemDocs));

            foreach (DocumentNode executable in executableDocs.Select(file => file.Document))
            {
                analyzer.AddDocument(executable);
            }

            return analyzer.Analyze();
        }
    }
}
