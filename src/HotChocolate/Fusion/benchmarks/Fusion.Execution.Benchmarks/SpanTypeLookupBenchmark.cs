using System;
using System.Collections.Generic;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using HotChocolate.Buffers;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Fusion.Types;
using HotChocolate.Types;

#nullable enable

namespace HotChocolate.Fusion.Execution.Benchmarks;

/// <summary>
/// Measures a hit-scenario <c>__typename</c> to object type lookup for a schema whose interface
/// has more than <c>FusionInterfaceTypeDefinition.MaxTypeNameLookupTypes</c> (4) implementers, so
/// callers fall back to <c>FusionTypeDefinitionCollection.GetType&lt;T&gt;</c>. Today that fallback
/// requires <c>SourceResultElement.AssertString()</c> (UTF-8 to UTF-16 transcode plus a heap
/// string allocation) before the string-keyed lookup runs. The candidate
/// <c>GetType&lt;T&gt;(ReadOnlySpan&lt;byte&gt;)</c> overload transcodes the UTF-8 name into a
/// stackalloc'd char buffer and probes a <c>FrozenDictionary</c> alternate lookup keyed by
/// <c>ReadOnlySpan&lt;char&gt;</c>, so a lookup hit never allocates a string.
/// </summary>
[MemoryDiagnoser]
[Config(typeof(BenchmarkConfig))]
public class SpanTypeLookupBenchmark
{
    /// <summary>
    /// BenchmarkDotNet 0.15.8 has no RuntimeMoniker for the net11.0 preview host and
    /// this project pins TargetFramework to net11.0, so out-of-process toolchains can
    /// neither validate nor build a child process here. The job therefore runs in
    /// process with the intended 3 warmup and 10 measurement iterations.
    /// </summary>
    private sealed class BenchmarkConfig : ManualConfig
    {
        public BenchmarkConfig()
            => AddJob(
                Job.Default
                    .WithWarmupCount(3)
                    .WithIterationCount(10)
                    .WithToolchain(InProcessEmitToolchain.Instance));
    }

    private const int ElementCount = 1000;

    private FusionSchemaDefinition _schema = null!;
    private MemoryArena _arena = null!;
    private SourceResultDocument _doc = null!;

    [GlobalSetup]
    public void Setup()
    {
        _schema = ComposeSchema();
        _arena = new MemoryArena();
        _doc = ParsePayload(BuildPayload());

        VerifyEquivalence();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _arena.Dispose();
    }

    [Benchmark(Baseline = true)]
    public int Baseline()
    {
        var total = 0;

        foreach (var element in _doc.Root.EnumerateArray())
        {
            var typeNameElement = element.GetProperty(IntrospectionFieldNames.TypeNameSpan);
            var typeName = typeNameElement.AssertString();
            var type = _schema.Types.GetType<IObjectTypeDefinition>(typeName);
            total += type.Name.Length;
        }

        return total;
    }

    [Benchmark]
    public int SpanLookup()
    {
        var total = 0;

        foreach (var element in _doc.Root.EnumerateArray())
        {
            var typeNameElement = element.GetProperty(IntrospectionFieldNames.TypeNameSpan);
            var rawTypeName = typeNameElement.AssertUtf8String();
            var type = _schema.Types.GetType<IObjectTypeDefinition>(rawTypeName);
            total += type.Name.Length;
        }

        return total;
    }

    private void VerifyEquivalence()
    {
        var baselineTypes = new List<IObjectTypeDefinition>(ElementCount);
        var spanTypes = new List<IObjectTypeDefinition>(ElementCount);

        foreach (var element in _doc.Root.EnumerateArray())
        {
            var typeNameElement = element.GetProperty(IntrospectionFieldNames.TypeNameSpan);

            var typeName = typeNameElement.AssertString();
            baselineTypes.Add(_schema.Types.GetType<IObjectTypeDefinition>(typeName));

            var rawTypeName = typeNameElement.AssertUtf8String();
            spanTypes.Add(_schema.Types.GetType<IObjectTypeDefinition>(rawTypeName));
        }

        if (baselineTypes.Count != ElementCount || spanTypes.Count != ElementCount)
        {
            throw new InvalidOperationException(
                $"Expected {ElementCount} resolved types but baseline produced "
                + $"{baselineTypes.Count} and span lookup produced {spanTypes.Count}.");
        }

        for (var i = 0; i < ElementCount; i++)
        {
            if (!ReferenceEquals(baselineTypes[i], spanTypes[i]))
            {
                throw new InvalidOperationException(
                    $"Type mismatch at element {i}: baseline resolved "
                    + $"'{baselineTypes[i].Name}' but span lookup resolved "
                    + $"'{spanTypes[i].Name}' (or a different instance).");
            }
        }
    }

    private SourceResultDocument ParsePayload(byte[] payload)
    {
        return SourceResultDocument.Parse(_arena, payload, payload.Length);
    }

    /// <summary>
    /// Every element carries the same <c>__typename</c>, which is the hit scenario the
    /// alternate-lookup path is meant to help: the name is always present in the schema.
    /// </summary>
    private static byte[] BuildPayload()
    {
        var json = new StringBuilder(ElementCount * 32);
        json.Append('[');

        for (var i = 0; i < ElementCount; i++)
        {
            if (i > 0)
            {
                json.Append(',');
            }

            json.Append("{\"__typename\":\"Product\",\"id\":\"id-").Append(i).Append("\"}");
        }

        json.Append(']');
        return Encoding.UTF8.GetBytes(json.ToString());
    }

    /// <summary>
    /// Composes a minimal fusion schema whose SearchResult interface has six implementers,
    /// which exceeds FusionInterfaceTypeDefinition.MaxTypeNameLookupTypes (4). The lookup under
    /// test targets the object type collection directly, so the interface's own lookup table
    /// size only needs to satisfy the ">4 implementers" schema-shape requirement.
    /// </summary>
    private static FusionSchemaDefinition ComposeSchema()
    {
        List<SourceSchemaText> sourceSchemas =
        [
            new SourceSchemaText(
                "search",
                """
                type Query {
                  searchContent(query: String!): [SearchResult!]!
                }

                interface SearchResult {
                  id: ID!
                  title: String!
                }

                type Product implements SearchResult {
                  id: ID!
                  title: String!
                }

                type Article implements SearchResult {
                  id: ID!
                  title: String!
                }

                type Brand implements SearchResult {
                  id: ID!
                  title: String!
                }

                type Category implements SearchResult {
                  id: ID!
                  title: String!
                }

                type Promotion implements SearchResult {
                  id: ID!
                  title: String!
                }

                type Vendor implements SearchResult {
                  id: ID!
                  title: String!
                }
                """)
        ];

        var compositionLog = new CompositionLog();
        var composerOptions = new SchemaComposerOptions();
        var composer = new SchemaComposer(sourceSchemas, composerOptions, compositionLog);
        var result = composer.Compose();

        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(result.Errors[0].Message);
        }

        return FusionSchemaDefinition.Create(result.Value.ToSyntaxNode());
    }
}
