using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using Basic.Reference.Assemblies;
using CookieCrumble;
using HotChocolate.Types.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace HotChocolate.Types;

internal static partial class TestHelper
{
    public static Snapshot GetGeneratedSourceSnapshot([StringSyntax("csharp")] string sourceText)
    {
        // Parse the provided string into a C# syntax tree.
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceText);

        IEnumerable<PortableExecutableReference> references = new[]
        {
            MetadataReference.CreateFromFile(typeof(DataLoaderAttribute).Assembly.Location)
        };

        // Create a Roslyn compilation for the syntax tree.
        var compilation = CSharpCompilation.Create(
            assemblyName: "Tests",
            syntaxTrees: new[] { syntaxTree },
            ReferenceAssemblies.Net80.Concat(references));

        // Create an instance of our GraphQLServerGenerator incremental source generator.
        var generator = new GraphQLServerGenerator();

        // The GeneratorDriver is used to run our generator against a compilation.
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        // Run the source generator.
        driver = driver.RunGenerators(compilation);

        // Create a snapshot.
        return CreateSnapshot(driver);
    }

    private static Snapshot CreateSnapshot(GeneratorDriver driver)
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

                snapshot.Add(text, source.HintName);
            }

            // Add diagnostics.
            if (result.Diagnostics.Any())
            {
                AddDiagnosticsToSnapshot(snapshot, result.Diagnostics);
            }
        }

        return snapshot;
    }

    private static void AddDiagnosticsToSnapshot(
        Snapshot snapshot,
        ImmutableArray<Diagnostic> diagnostics)
    {
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

        snapshot.Add(Encoding.UTF8.GetString(stream.ToArray()), "Diagnostics");
    }

    [GeneratedRegex("MiddlewareFactories([a-z0-9]{32})")]
    private static partial Regex MiddlewareFactoryHashRegex();
}
