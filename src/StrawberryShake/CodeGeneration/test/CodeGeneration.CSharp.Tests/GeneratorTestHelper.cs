using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
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
        public static void AssertResult(params string[] sourceTexts)
        {
            ClientModel clientModel = CreateClientModel(sourceTexts);

            var documents = new StringBuilder();
            var documentNames = new HashSet<string>();
            var generator = new CSharpGeneratorExecutor();

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

            foreach (CSharpDocument document in generator.Generate(clientModel, "Foo", "FooClient"))
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

        private static ClientModel CreateClientModel(params string[] sourceText)
        {
            var documents = sourceText
                .Select(sourceText => (string.Empty, Utf8GraphQLParser.Parse(sourceText)))
                .ToList();

            var typeSystemDocs = documents.GetTypeSystemDocuments().ToList();
            var executableDocs = documents.GetExecutableDocuments().ToList();

            var analyzer = new DocumentAnalyzer();

            analyzer.SetSchema(SchemaHelper.Load(typeSystemDocs));

            foreach (DocumentNode executable in executableDocs.Select(doc => doc.document))
            {
                analyzer.AddDocument(executable);
            }

            return analyzer.Analyze();
        }
    }
}
