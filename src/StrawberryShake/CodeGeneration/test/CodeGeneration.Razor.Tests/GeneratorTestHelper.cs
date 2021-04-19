using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.CodeAnalysis;
using ChilliCream.Testing;
using HotChocolate;
using HotChocolate.Language;
using Snapshooter;
using Snapshooter.Xunit;
using StrawberryShake.CodeGeneration.Analyzers;
using StrawberryShake.CodeGeneration.Analyzers.Models;
using StrawberryShake.CodeGeneration.Utilities;
using Xunit;
using Snapshot = Snapshooter.Xunit.Snapshot;
using RequestStrategyGen = StrawberryShake.Tools.Configuration.RequestStrategy;
using static StrawberryShake.CodeGeneration.CSharp.CSharpGenerator;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public static class GeneratorTestHelper
    {
        public static IReadOnlyList<IError> AssertError(params string[] fileNames)
        {
            CSharpGeneratorResult result = Generate(
                fileNames,
                new CSharpGeneratorSettings { Namespace = "Foo.Bar", ClientName = "FooClient" });

            Assert.True(
                result.Errors.Any(),
                "It is expected that the result has no generator errors!");

            return result.Errors;
        }

        public static void AssertResult(params string[] sourceTexts) =>
            AssertResult(true, sourceTexts);

        public static void AssertResult(
            bool strictValidation,
            params string[] sourceTexts) =>
            AssertResult(
                new AssertSettings { StrictValidation = strictValidation },
                sourceTexts);

        public static void AssertResult(
            AssertSettings settings,
            params string[] sourceTexts)
        {
            AssertResult(settings, false, sourceTexts);
        }

        public static void AssertResult(
            AssertSettings settings,
            bool skipWarnings,
            params string[] sourceTexts)
        {
            ClientModel clientModel =
                CreateClientModel(sourceTexts, settings.StrictValidation, settings.NoStore);

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

            if (settings.Profiles.Count == 0)
            {
                settings.Profiles.Add(TransportProfile.Default);
            }

            CSharpGeneratorResult result = Generate(
                clientModel,
                new CSharpGeneratorSettings
                {
                    Namespace = settings.Namespace ?? "Foo.Bar",
                    ClientName = settings.ClientName ?? "FooClient",
                    StrictSchemaValidation = settings.StrictValidation,
                    RequestStrategy = settings.RequestStrategy,
                    TransportProfiles = settings.Profiles,
                    NoStore = settings.NoStore,
                    InputRecords = settings.InputRecords,
                    EntityRecords = settings.EntityRecords,
                    RazorComponents = settings.RazorComponents
                });

            Assert.False(
                result.Errors.Any(),
                "It is expected that the result has no generator errors!");

            foreach (var document in result.Documents)
            {
                if (!documentNames.Add($"{document.Name}.{document.Kind}"))
                {
                    Assert.True(false, $"Document name duplicated {document.Name}");
                }

                if (document.Kind is SourceDocumentKind.CSharp or SourceDocumentKind.Razor)
                {
                    documents.AppendLine("// " + document.Name);
                    documents.AppendLine();
                    documents.AppendLine(document.SourceText);
                    documents.AppendLine();
                }
                else if (document.Kind is SourceDocumentKind.GraphQL)
                {
                    documents.AppendLine("// " + document.Name);
                    documents.AppendLine("// " + document.Hash);
                    documents.AppendLine();

                    using var reader = new StringReader(document.SourceText);
                    string? line;

                    do
                    {
                        line = reader.ReadLine();
                        if (line is not null)
                        {
                            documents.AppendLine("// " + line);
                        }
                    } while (line is not null);

                    documents.AppendLine();
                }
            }

            if (settings.SnapshotFile is not null)
            {
                documents.ToString()
                    .MatchSnapshot(
                        new SnapshotFullName(
                            settings.SnapshotFile,
                            Snapshot.FullName().FolderPath));
            }
            else
            {
                documents.ToString().MatchSnapshot();
            }

            IReadOnlyList<Diagnostic> diagnostics =
                CSharpCompiler.GetDiagnosticErrors(documents.ToString());

            if (skipWarnings)
            {
                diagnostics = diagnostics
                    .Where(x => x.Severity == DiagnosticSeverity.Error)
                    .ToList();
            }

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

        public static void AssertStarWarsResult(params string[] sourceTexts) =>
            AssertStarWarsResult(
                new AssertSettings { StrictValidation = true },
                sourceTexts);


        public static void AssertStarWarsResult(
            AssertSettings settings,
            params string[] sourceTexts)
        {
            var source = new string[sourceTexts.Length + 2];

            source[0] = FileResource.Open("Schema.graphql");
            source[1] = FileResource.Open("Schema.extensions.graphql");

            Array.Copy(
                sourceTexts,
                sourceIndex: 0,
                source,
                destinationIndex: 2,
                length: sourceTexts.Length);

            AssertResult(settings, true, source);
        }

        public static AssertSettings CreateIntegrationTest(
            RequestStrategyGen requestStrategy = RequestStrategyGen.Default,
            TransportProfile[]? profiles = null,
            bool noStore = false,
            [CallerMemberName] string? testName = null)
        {
            SnapshotFullName snapshotFullName = Snapshot.FullName();
            string testFile = System.IO.Path.Combine(
                snapshotFullName.FolderPath,
                testName + "Test.cs");
            string ns = "StrawberryShake.CodeGeneration.CSharp.Integration." + testName;

            if (!File.Exists(testFile))
            {
                File.WriteAllText(
                    testFile,
                    FileResource.Open("TestTemplate.txt")
                        .Replace("{TestName}", testName)
                        .Replace("{Namespace}", ns));
            }

            return new AssertSettings
            {
                ClientName = testName! + "Client",
                Namespace = ns,
                StrictValidation = true,
                SnapshotFile = System.IO.Path.Combine(
                    snapshotFullName.FolderPath,
                    testName + "Test.Client.cs"),
                RequestStrategy = requestStrategy,
                NoStore = noStore,
                Profiles = (profiles ?? new[]
                {
                    TransportProfile.Default
                }).ToList()
            };
        }

        private static ClientModel CreateClientModel(
            string[] sourceText,
            bool strictValidation,
            bool noStore)
        {
            var files = sourceText
                .Select(s => new GraphQLFile(Utf8GraphQLParser.Parse(s)))
                .ToList();

            var typeSystemDocs = files.GetTypeSystemDocuments().ToList();
            var executableDocs = files.GetExecutableDocuments().ToList();

            var analyzer = new DocumentAnalyzer();

            analyzer.SetSchema(SchemaHelper.Load(typeSystemDocs, strictValidation, noStore));

            foreach (DocumentNode executable in executableDocs.Select(file => file.Document))
            {
                analyzer.AddDocument(executable);
            }

            return analyzer.Analyze();
        }

        public class AssertSettings
        {
            public string? ClientName { get; set; }

            public string? Namespace { get; set; }

            public bool StrictValidation { get; set; }

            public string? SnapshotFile { get; set; }

            public bool NoStore { get; set; }

            public bool InputRecords { get; set; }

            public bool EntityRecords { get; set; }

            public bool RazorComponents { get; set; }

            public List<TransportProfile> Profiles { get; set; } = new();

            public RequestStrategyGen RequestStrategy { get; set; } =
                RequestStrategyGen.Default;
        }
    }
}
