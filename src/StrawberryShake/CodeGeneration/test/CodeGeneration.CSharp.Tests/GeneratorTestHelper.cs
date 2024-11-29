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
using Snapshot = Snapshooter.Xunit.Snapshot;
using RequestStrategyGen = StrawberryShake.Tools.Configuration.RequestStrategy;
using static StrawberryShake.CodeGeneration.CSharp.CSharpGenerator;

namespace StrawberryShake.CodeGeneration.CSharp;

public static class GeneratorTestHelper
{
    public static IReadOnlyList<IError> AssertError(params string[] fileNames)
    {
        var result = GenerateAsync(
            fileNames,
            new CSharpGeneratorSettings
            {
                Namespace = "Foo.Bar",
                ClientName = "FooClient",
                AccessModifier = AccessModifier.Public,
            })
            .Result;

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
            new AssertSettings { StrictValidation = strictValidation, },
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
        var clientModel =
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

        var result = Generate(
            clientModel,
            new CSharpGeneratorSettings
            {
                Namespace = settings.Namespace ?? "Foo.Bar",
                ClientName = settings.ClientName ?? "FooClient",
                AccessModifier = settings.AccessModifier,
                StrictSchemaValidation = settings.StrictValidation,
                RequestStrategy = settings.RequestStrategy,
                TransportProfiles = settings.Profiles,
                NoStore = settings.NoStore,
                InputRecords = settings.InputRecords,
                EntityRecords = settings.EntityRecords,
                RazorComponents = settings.RazorComponents,
            });

        Assert.False(
            result.Errors.Any(),
            "It is expected that the result has no generator errors!");

        foreach (var document in result.Documents)
        {
            if (!documentNames.Add(document.Name))
            {
                Assert.Fail($"Document name duplicated {document.Name}");
            }

            if (document.Kind == SourceDocumentKind.CSharp)
            {
                documents.AppendLine("// " + document.Name);
                documents.AppendLine();
                documents.AppendLine(document.SourceText);
                documents.AppendLine();
            }
            else if (document.Kind == SourceDocumentKind.GraphQL)
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

        var diagnostics = CSharpCompiler.GetDiagnosticErrors(documents.ToString());

        if (skipWarnings)
        {
            diagnostics = diagnostics
                .Where(x => x.Severity == DiagnosticSeverity.Error)
                .ToList();
        }

        if (diagnostics.Any())
        {
            Assert.Fail("Diagnostic Errors: \n" +
                diagnostics
                    .Select(x =>
                        $"{x.GetMessage()}" +
                        $" (Line: {x.Location.GetLineSpan().StartLinePosition.Line})")
                    .Aggregate((acc, val) => acc + "\n" + val));
        }
    }

    public static void AssertStarWarsResult(params string[] sourceTexts) =>
        AssertStarWarsResult(
            new AssertSettings { StrictValidation = true, },
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
        AccessModifier accessModifier = AccessModifier.Public,
        bool noStore = false,
        [CallerMemberName] string? testName = null)
    {
        var snapshotFullName = Snapshot.FullName();
        var testFile = System.IO.Path.Combine(
            snapshotFullName.FolderPath,
            testName + "Test.cs");
        var ns = "StrawberryShake.CodeGeneration.CSharp.Integration." + testName;

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
            AccessModifier = accessModifier,
            StrictValidation = true,
            SnapshotFile = System.IO.Path.Combine(
                snapshotFullName.FolderPath,
                testName + "Test.Client.cs"),
            RequestStrategy = requestStrategy,
            NoStore = noStore,
            Profiles = (profiles ??
            [
                TransportProfile.Default,
            ]).ToList(),
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

        foreach (var executable in executableDocs.Select(file => file.Document))
        {
            analyzer.AddDocument(executable);
        }

        return analyzer.AnalyzeAsync().Result;
    }

    public class AssertSettings
    {
        public string? ClientName { get; set; }

        public string? Namespace { get; set; }

        public AccessModifier AccessModifier { get; set; }
            = AccessModifier.Public;

        public bool StrictValidation { get; set; }

        public string? SnapshotFile { get; set; }

        public bool NoStore { get; set; }

        public bool InputRecords { get; set; }

        public bool EntityRecords { get; set; }

        public bool RazorComponents { get; set; }

        public List<TransportProfile> Profiles { get; set; } = [];

        public RequestStrategyGen RequestStrategy { get; set; } =
            RequestStrategyGen.Default;
    }
}
