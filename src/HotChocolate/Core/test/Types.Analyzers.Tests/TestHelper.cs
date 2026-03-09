using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using Basic.Reference.Assemblies;
using GreenDonut;
using GreenDonut.Data;
using HotChocolate.Data.Filters;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Execution.Processing;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Types.Analyzers;
using HotChocolate.Types.Pagination;
using Microsoft.AspNetCore.Builder;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types;

internal static partial class TestHelper
{
    private static readonly HashSet<string> s_ignoreCodes = ["CS8652", "CS8632", "CS5001", "CS8019"];

    public static Snapshot GetGeneratedSourceSnapshot([StringSyntax("csharp")] string sourceText)
        => GetGeneratedSourceSnapshot([sourceText]);

    public static Snapshot GetGeneratedSourceSnapshot(
        string[] sourceTexts,
        string? assemblyName = "Tests",
        bool enableInterceptors = false,
        bool enableAnalyzers = false)
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
            // HotChocolate.Primitives
            MetadataReference.CreateFromFile(typeof(ITypeSystemMember).Assembly.Location),

            // HotChocolate.Execution
            MetadataReference.CreateFromFile(typeof(RequestDelegate).Assembly.Location),

            // HotChocolate.Execution.Abstractions
            MetadataReference.CreateFromFile(typeof(RequestContext).Assembly.Location),

            // HotChocolate.Execution.Processing
            MetadataReference.CreateFromFile(typeof(HotChocolateExecutionSelectionExtensions).Assembly.Location),

            // HotChocolate.Execution.Abstractions
            MetadataReference.CreateFromFile(typeof(IRequestExecutorBuilder).Assembly.Location),

            // HotChocolate.Execution.Operation.Abstractions
            MetadataReference.CreateFromFile(typeof(ISelection).Assembly.Location),

            // HotChocolate.Types
            MetadataReference.CreateFromFile(typeof(ObjectTypeAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Connection).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(PageConnection<>).Assembly.Location),

            // HotChocolate.Types.Abstractions
            MetadataReference.CreateFromFile(typeof(ISchemaDefinition).Assembly.Location),

            // HotChocolate.Features
            MetadataReference.CreateFromFile(typeof(IFeatureProvider).Assembly.Location),

            // HotChocolate.Language
            MetadataReference.CreateFromFile(typeof(OperationType).Assembly.Location),

            // HotChocolate.Abstractions
            MetadataReference.CreateFromFile(typeof(ParentAttribute).Assembly.Location),

            // HotChocolate.AspNetCore
            MetadataReference.CreateFromFile(
                typeof(HotChocolateAspNetCoreServiceCollectionExtensions).Assembly.Location),

            // GreenDonut
            MetadataReference.CreateFromFile(typeof(DataLoaderBase<,>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(IDataLoader).Assembly.Location),

            // GreenDonut.Data
            MetadataReference.CreateFromFile(typeof(PagingArguments).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(IPredicateBuilder).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(DefaultPredicateBuilder).Assembly.Location),

            // HotChocolate.Data
            MetadataReference.CreateFromFile(typeof(IFilterContext).Assembly.Location),

            // Microsoft.AspNetCore
            MetadataReference.CreateFromFile(typeof(WebApplication).Assembly.Location),

            // Microsoft.Extensions.DependencyInjection.Abstractions
            MetadataReference.CreateFromFile(typeof(IServiceCollection).Assembly.Location),

            // Microsoft.AspNetCore.Authorization
            MetadataReference.CreateFromFile(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute).Assembly.Location),

            // HotChocolate.Authorization
            MetadataReference.CreateFromFile(typeof(Authorization.AuthorizeAttribute).Assembly.Location)
        ];

        // Create a Roslyn compilation for the syntax tree.
        var parseOptions = !enableInterceptors
            ? CSharpParseOptions.Default
            : CSharpParseOptions.Default
                .WithPreprocessorSymbols("InterceptorsPreviewFeature")
                .WithFeatures(new Dictionary<string, string>
                {
                    ["InterceptorsNamespaces"] = "HotChocolate.Execution.Generated"
                });

        var compilation = CSharpCompilation.Create(
            assemblyName: assemblyName,
            syntaxTrees: sourceTexts.Select(s => CSharpSyntaxTree.ParseText(s, parseOptions)),
            references);

        // Create an instance of our GraphQLServerGenerator incremental source generator.
        var generator = new GraphQLServerGenerator();

        // The GeneratorDriver is used to run our generator against a compilation.
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        // Run the source generator.
        driver = driver.RunGenerators(compilation);

        // Create a snapshot.
        var snapshot = CreateSnapshot(compilation, driver, enableAnalyzers);

        // Finally, compile the entire assembly (original code + generated code) to check
        // if the sample is valid as a whole
        var updatedCompilation = compilation.AddSyntaxTrees(
            driver.GetRunResult()
                .Results
                .SelectMany(r => r.GeneratedSources)
                .OrderBy(gs => gs.HintName)
                .Select(gs => CSharpSyntaxTree.ParseText(gs.SourceText, parseOptions, path: gs.HintName))
        );

        using var dllStream = new MemoryStream();
        var emitResult = updatedCompilation.Emit(dllStream);
        if (!emitResult.Success || emitResult.Diagnostics.Any())
        {
            AddDiagnosticsToSnapshot(snapshot, emitResult.Diagnostics, "Assembly Emit Diagnostics");
        }

        return snapshot;
    }

    private static Snapshot CreateSnapshot(CSharpCompilation compilation, GeneratorDriver driver, bool enableAnalyzers)
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
            if (diagnostics.Length > 0)
            {
                AddDiagnosticsToSnapshot(snapshot, diagnostics, "Compilation Diagnostics");
            }

            if (result.Diagnostics.Any())
            {
                AddDiagnosticsToSnapshot(snapshot, result.Diagnostics, "Generator Diagnostics");
            }
        }

        // Run diagnostic analyzers if enabled
        if (enableAnalyzers)
        {
            var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(
                new RootTypePartialAnalyzer(),
                new NodeResolverIdAttributeAnalyzer(),
                new NodeResolverPublicAnalyzer(),
                new NodeResolverIdParameterAnalyzer(),
                new BindMemberAnalyzer(),
                new ExtendObjectTypeAnalyzer(),
                new ParentAttributeAnalyzer(),
                new ParentMethodAnalyzer(),
                new QueryContextProjectionAnalyzer(),
                new QueryContextConnectionAnalyzer(),
                new ShareableInterfaceTypeAnalyzer(),
                new ShareableScopedOnMemberAnalyzer(),
                new DataAttributeOrderAnalyzer(),
                new IdAttributeOnRecordParameterAnalyzer(),
                new WrongAuthorizationAttributeAnalyzer());

            var compilationWithAnalyzers = compilation.WithAnalyzers(analyzers);
            var analyzerDiagnostics = compilationWithAnalyzers.GetAllDiagnosticsAsync().Result;

            if (analyzerDiagnostics.Any())
            {
                AddDiagnosticsToSnapshot(snapshot, analyzerDiagnostics, "Analyzer Diagnostics");
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

    [GeneratedRegex("MiddlewareFactories([a-z0-9]{32})")]
    private static partial Regex MiddlewareFactoryHashRegex();

    internal static class ForceInvariantDefaultCultureModuleInitializer
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            // Compile errors are localized, so enforce a common default culture,
            // since otherwise the snapshot comparison may fail
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
        }
    }
}
