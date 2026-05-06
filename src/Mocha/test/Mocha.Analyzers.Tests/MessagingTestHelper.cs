using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using CookieCrumble;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Mocha.Analyzers.Tests;

internal static class MessagingTestHelper
{
    private static readonly HashSet<string> s_ignoreCodes =
        ["CS8652", "CS8632", "CS5001", "CS8019", "CS0518", "CS0012"];

    public static Snapshot GetGeneratedSourceSnapshot(
        string[] sourceTexts,
        string? assemblyName = "Tests",
        bool publishAot = false)
    {
        var driver = RunGenerator(sourceTexts, assemblyName, publishAot);
        var snapshot = new Snapshot();

        foreach (var result in driver.GetRunResult().Results)
        {
            var sources = result.GeneratedSources.OrderBy(s => s.HintName);
            foreach (var source in sources)
            {
                snapshot.Add(source.SourceText.ToString(), source.HintName, MarkdownLanguages.CSharp);
            }

            if (result.Diagnostics.Any())
            {
                AddDiagnosticsToSnapshot(snapshot, result.Diagnostics, "Generator Diagnostics");
            }
        }

        return snapshot;
    }

    public static ImmutableArray<Diagnostic> GetGeneratorDiagnostics(
        string[] sourceTexts,
        string? assemblyName = "Tests",
        bool publishAot = false)
    {
        var driver = RunGenerator(sourceTexts, assemblyName, publishAot);

        return driver.GetRunResult().Results
            .SelectMany(static r => r.Diagnostics)
            .ToImmutableArray();
    }

    public static ImmutableArray<string> GetGeneratedSourceTexts(
        string[] sourceTexts,
        string? assemblyName = "Tests",
        bool publishAot = false)
    {
        var driver = RunGenerator(sourceTexts, assemblyName, publishAot);

        return driver.GetRunResult().Results
            .SelectMany(static r => r.GeneratedSources)
            .OrderBy(static s => s.HintName)
            .Select(static s => s.SourceText.ToString())
            .ToImmutableArray();
    }

    private static GeneratorDriver RunGenerator(
        string[] sourceTexts,
        string? assemblyName,
        bool publishAot)
    {
        IEnumerable<PortableExecutableReference> references =
        [
#if NET8_0
            .. Net80.References.All,
#elif NET9_0
            .. Net90.References.All,
#elif NET10_0
            .. Net100.References.All,
#endif
            // Mocha.Abstractions (IEventHandler, IEventRequestHandler, IEventRequest, MessagingModuleAttribute)
            MetadataReference.CreateFromFile(typeof(IEventHandler).Assembly.Location),

            // Mocha (IConsumer, IBatchEventHandler, Saga, SagaStateBase)
            MetadataReference.CreateFromFile(typeof(IConsumer).Assembly.Location),

            // Microsoft.Extensions.DependencyInjection.Abstractions
            MetadataReference.CreateFromFile(typeof(IServiceCollection).Assembly.Location),

            // System.Runtime.CompilerServices.Unsafe
            MetadataReference.CreateFromFile(typeof(Unsafe).Assembly.Location),

            // System.Runtime from the actual runtime (needed for predefined type resolution
            // so that assembly-level attribute constructor arguments can be bound)
            MetadataReference.CreateFromFile(
                Path.Combine(
                    Path.GetDirectoryName(typeof(object).Assembly.Location)!,
                    "System.Runtime.dll"))
        ];

        var compilation = CSharpCompilation.Create(
            assemblyName: assemblyName,
            syntaxTrees: sourceTexts.Select(s => CSharpSyntaxTree.ParseText(s)),
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new MessagingGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator)
            .WithUpdatedAnalyzerConfigOptions(new TestAnalyzerConfigOptionsProvider(publishAot));

        return driver.RunGenerators(compilation);
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

        foreach (var diagnostic in diagnostics
            .OrderBy(d => d.Location.SourceTree?.FilePath)
            .ThenBy(d => d.Location.GetLineSpan().StartLinePosition.Line)
            .ThenBy(d => d.Location.GetLineSpan().StartLinePosition.Character))
        {
            if (s_ignoreCodes.Contains(diagnostic.Id))
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

    private sealed class TestAnalyzerConfigOptionsProvider(bool publishAot)
        : AnalyzerConfigOptionsProvider
    {
        private readonly AnalyzerConfigOptions _globalOptions =
            new TestAnalyzerConfigOptions(
                new Dictionary<string, string>
                {
                    ["build_property.PublishAot"] = publishAot ? "true" : "false"
                });

        public override AnalyzerConfigOptions GlobalOptions => _globalOptions;

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree)
            => TestAnalyzerConfigOptions.Empty;

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
            => TestAnalyzerConfigOptions.Empty;
    }

    private sealed class TestAnalyzerConfigOptions(IReadOnlyDictionary<string, string> options)
        : AnalyzerConfigOptions
    {
        public static readonly AnalyzerConfigOptions Empty =
            new TestAnalyzerConfigOptions(new Dictionary<string, string>());

        public override bool TryGetValue(string key, out string value)
        {
            if (options.TryGetValue(key, out var result))
            {
                value = result;
                return true;
            }

            value = "";
            return false;
        }
    }
}
