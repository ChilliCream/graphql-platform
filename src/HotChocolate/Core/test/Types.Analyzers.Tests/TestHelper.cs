using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using Basic.Reference.Assemblies;
using GreenDonut;
using HotChocolate.Pagination;
using HotChocolate.Types.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace HotChocolate.Types;

internal static partial class TestHelper
{
    private static HashSet<string> _ignoreCodes = ["CS8652", "CS8632", "CS5001", "CS8019"];

    public static Snapshot GetGeneratedSourceSnapshot([StringSyntax("csharp")] string sourceText)
    {
        return GetGeneratedSourceSnapshot([sourceText]);
    }

    public static Snapshot GetGeneratedSourceSnapshot(string[] sourceTexts)
    {
        IEnumerable<PortableExecutableReference> references =
        [
#if NET8_0
            .. Net80.References.All,
#elif NET9_0
            .. Net90.References.All,
#endif

            // HotChocolate.Types
            MetadataReference.CreateFromFile(typeof(ObjectTypeAttribute).Assembly.Location),

            // HotChocolate.Abstractions
            MetadataReference.CreateFromFile(typeof(ParentAttribute).Assembly.Location),

            // HotChocolate.Pagination.Primitives
            MetadataReference.CreateFromFile(typeof(PagingArguments).Assembly.Location),

            // GreenDonut
            MetadataReference.CreateFromFile(typeof(DataLoaderAttribute).Assembly.Location)
        ];

        // Create a Roslyn compilation for the syntax tree.
        var compilation = CSharpCompilation.Create(
            assemblyName: "Tests",
            syntaxTrees: sourceTexts.Select(s => CSharpSyntaxTree.ParseText(s)),
            references);

        // Create an instance of our GraphQLServerGenerator incremental source generator.
        var generator = new GraphQLServerGenerator();

        // The GeneratorDriver is used to run our generator against a compilation.
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        // Run the source generator.
        driver = driver.RunGenerators(compilation);

        // Create a snapshot.
        return CreateSnapshot(compilation, driver);
    }

    private static Snapshot CreateSnapshot(CSharpCompilation compilation, GeneratorDriver driver)
    {
        var snapshot = new Snapshot();

        foreach (var result in driver.GetRunResult().Results)
        {
            // Add sources.
            var sources = result.GeneratedSources.OrderBy(s => s.HintName);

            foreach (var source in sources)
            {
                var text = source.SourceText.ToString();

                // Replace variable hash in class name.
                if (source.HintName.StartsWith("HotChocolateMiddleware"))
                {
                    text = MiddlewareFactoryHashRegex().Replace(
                        text,
                        m => m.Value.Replace(m.Groups[1].Value, "HASH"));
                }

                snapshot.Add(text, source.HintName, MarkdownLanguages.CSharp);
            }

            // Add diagnostics.
            var diagnostics = compilation.GetDiagnostics();
            if(diagnostics.Length > 0)
            {
                AddDiagnosticsToSnapshot(snapshot, diagnostics, "Compilation Diagnostics");
            }

            if (result.Diagnostics.Any())
            {
                AddDiagnosticsToSnapshot(snapshot, result.Diagnostics, "Generator Diagnostics");
            }
        }

        return snapshot;
    }

    private static void AddDiagnosticsToSnapshot(
        Snapshot snapshot,
        ImmutableArray<Diagnostic> diagnostics,
        string title)
    {
        var hasDiagnostics = false;

        using var stream = new MemoryStream();
        using var jsonWriter = new Utf8JsonWriter(
            stream,
            new JsonWriterOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                Indented = true
            });

        jsonWriter.WriteStartArray();

        foreach (var diagnostic in diagnostics)
        {
            if(_ignoreCodes.Contains(diagnostic.Id))
            {
                continue;
            }

            hasDiagnostics = true;

            jsonWriter.WriteStartObject();
            jsonWriter.WriteString(nameof(diagnostic.Id), diagnostic.Id);

            var descriptor = diagnostic.Descriptor;

            jsonWriter.WriteString(nameof(descriptor.Title), descriptor.Title.ToString());
            jsonWriter.WriteString(nameof(diagnostic.Severity), diagnostic.Severity.ToString());
            jsonWriter.WriteNumber(nameof(diagnostic.WarningLevel), diagnostic.WarningLevel);

            jsonWriter.WriteString(
                nameof(diagnostic.Location),
                diagnostic.Location.GetMappedLineSpan().ToString());

            var description = descriptor.Description.ToString();
            if (!string.IsNullOrWhiteSpace(description))
            {
                jsonWriter.WriteString(nameof(descriptor.Description), description);
            }

            var help = descriptor.HelpLinkUri;
            if (!string.IsNullOrWhiteSpace(help))
            {
                jsonWriter.WriteString(nameof(descriptor.HelpLinkUri), help);
            }

            jsonWriter.WriteString(
                nameof(descriptor.MessageFormat),
                descriptor.MessageFormat.ToString());

            jsonWriter.WriteString("Message", diagnostic.GetMessage());
            jsonWriter.WriteString(nameof(descriptor.Category), descriptor.Category);

            jsonWriter.WritePropertyName(nameof(descriptor.CustomTags));

            jsonWriter.WriteStartArray();

            foreach (var tag in descriptor.CustomTags)
            {
                jsonWriter.WriteStringValue(tag);
            }

            jsonWriter.WriteEndArray();

            jsonWriter.WriteEndObject();
        }

        jsonWriter.WriteEndArray();
        jsonWriter.Flush();

        if (hasDiagnostics)
        {
            snapshot.Add(Encoding.UTF8.GetString(stream.ToArray()), title, MarkdownLanguages.Json);
        }
    }

    [GeneratedRegex("MiddlewareFactories([a-z0-9]{32})")]
    private static partial Regex MiddlewareFactoryHashRegex();
}
