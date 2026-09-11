using System.Text.Json;
using System.Text.Json.Serialization;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// Executes the six graphql-lean cost-precision cases (plus their named-fragment and
/// merged-control twins) as HotChocolate operations, from the same JSON fixtures the
/// Core conformance suite reads. Every fixture's <c>extensions.operationCost</c> must
/// equal its <c>expected</c> typeCost/fieldCost pair: this is the executable definition
/// of done for HotChocolate 6/6 (hc-3-mmh.3).
/// </summary>
public sealed class ArticleCasesTests
{
    public static TheoryData<string> ArticleFixturePaths => DiscoverArticleFixturePaths();

    [Theory]
    [MemberData(nameof(ArticleFixturePaths))]
    public async Task Fixture_Should_ReportExpectedOperationCost_When_Evaluated(string path)
    {
        // arrange
        var fixture = ArticleFixture.Load(path);
        var snapshot = new Snapshot(postFix: fixture.Id);

        var requestBuilder =
            OperationRequestBuilder.New()
                .SetDocument(fixture.Operation)
                .SetOperationName(fixture.OperationName)
                .ReportCost();

        if (fixture.Variables is { } variables)
        {
            requestBuilder.SetVariableValues(
                variables.EnumerateObject().Select(p => new KeyValuePair<string, JsonElement>(p.Name, p.Value)));
        }

        var requestExecutorBuilder =
            new ServiceCollection()
                .AddGraphQLServer()
                .AddDocumentFromString(StripBuiltInDirectiveDeclarations(fixture.Sdl))
                .ModifyCostOptions(o =>
                {
                    o.DefaultResolverCost = null;
                    o.ApplyCostDefaults = false;
                    o.DefaultListSize = fixture.DefaultListSize;
                });

        AddStubResolvers(requestExecutorBuilder, fixture.Id);
        var requestExecutor =
            await requestExecutorBuilder
                .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await requestExecutor.ExecuteAsync(requestBuilder.Build(), TestContext.Current.CancellationToken);
        var operationCost = (IReadOnlyDictionary<string, object?>)result.ExpectOperationResult().Extensions["operationCost"]!;

        // assert
        Assert.Equal(fixture.Expected.TypeCost, Convert.ToDouble(operationCost["typeCost"]));
        Assert.Equal(fixture.Expected.FieldCost, Convert.ToDouble(operationCost["fieldCost"]));
        await snapshot
            .Add(fixture.Operation, "Operation")
            .Add(fixture.Expected, "Expected")
            .Add(operationCost, "OperationCost")
            .MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    private static TheoryData<string> DiscoverArticleFixturePaths()
    {
        var data = new TheoryData<string>();
        var directory = System.IO.Path.Combine("__resources__", "article");

        foreach (var path in Directory.EnumerateFiles(directory, "*.json").OrderBy(p => p, StringComparer.Ordinal))
        {
            data.Add(path);
        }

        return data;
    }

    private static void AddStubResolvers(IRequestExecutorBuilder builder, string fixtureId)
    {
        if (fixtureId.StartsWith("c1-", StringComparison.Ordinal))
        {
            builder
                .AddResolver("Query", "result", _ => default(object))
                .AddResolver("A", "a", _ => 0)
                .AddResolver("B", "b", _ => 0);
        }
        else if (fixtureId.StartsWith("c2-", StringComparison.Ordinal))
        {
            builder
                .AddResolver("Query", "results", _ => Array.Empty<object>())
                .AddResolver("A", "a", _ => 0)
                .AddResolver("B", "b", _ => 0);
        }
        else if (fixtureId.StartsWith("c3-", StringComparison.Ordinal))
        {
            builder
                .AddResolver("Query", "left", _ => default(object))
                .AddResolver("Query", "right", _ => default(object))
                .AddResolver("Side", "costly", _ => 0);
        }
        else if (fixtureId.StartsWith("c4-", StringComparison.Ordinal))
        {
            builder
                .AddResolver("Query", "result", _ => default(object))
                .AddResolver("A", "a", _ => 0);
        }
        else if (fixtureId.StartsWith("c5-", StringComparison.Ordinal))
        {
            builder
                .AddResolver("Query", "book", _ => default(object))
                .AddResolver("Book", "title", _ => "")
                .AddResolver("Book", "author", _ => default(object))
                .AddResolver("Author", "name", _ => "")
                .AddType(new AnyType("Text"));
        }
        else if (fixtureId.StartsWith("c6-", StringComparison.Ordinal))
        {
            builder
                .AddResolver("Query", "items", _ => Array.Empty<object>())
                .AddResolver("Item", "value", _ => 0);
        }
    }

    // The fixture SDL declares `directive @cost`/`directive @listSize` so the Core
    // conformance suite's raw SchemaParser has something to bind against. HC's schema
    // builder already registers both directives through AddCostAnalyzer, so the
    // declarations are stripped here to avoid a duplicate directive definition; only the
    // directive usages on the fixture's types and fields survive.
    private static string StripBuiltInDirectiveDeclarations(string sdl)
        => string.Join(
            '\n',
            sdl.Split('\n')
                .Where(line =>
                    !line.StartsWith("directive @cost", StringComparison.Ordinal)
                    && !line.StartsWith("directive @listSize", StringComparison.Ordinal)));
}

file sealed record ArticleFixture(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("sdl")] string Sdl,
    [property: JsonPropertyName("operation")] string Operation,
    [property: JsonPropertyName("operationName")] string? OperationName,
    [property: JsonPropertyName("variables")] JsonElement? Variables,
    [property: JsonPropertyName("defaultListSize")] JsonElement DefaultListSizeRaw,
    [property: JsonPropertyName("expected")] ArticleFixtureExpected Expected)
{
    public double DefaultListSize
        => DefaultListSizeRaw.ValueKind == JsonValueKind.String
            ? double.PositiveInfinity
            : DefaultListSizeRaw.GetDouble();

    public static ArticleFixture Load(string path)
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<ArticleFixture>(json)
            ?? throw new InvalidOperationException($"Fixture '{path}' deserialized to null.");
    }
}

file sealed record ArticleFixtureExpected(
    [property: JsonPropertyName("typeCost")] double TypeCost,
    [property: JsonPropertyName("fieldCost")] double FieldCost);
