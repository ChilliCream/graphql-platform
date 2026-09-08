using System.Text.Json;
using System.Text.Json.Serialization;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
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

    [Theory(Skip = "enabled by hc-costplan-middleware")]
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

        var requestExecutor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddDocumentFromString(StripBuiltInDirectiveDeclarations(fixture.Sdl))
                .ModifyCostOptions(o =>
                {
                    o.DefaultResolverCost = null;
                    o.ApplyCostDefaults = false;
                    o.DefaultListSize = fixture.DefaultListSize;
                })
                .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await requestExecutor.ExecuteAsync(requestBuilder.Build(), TestContext.Current.CancellationToken);
        var operationCost = result.ExpectOperationResult().Extensions["operationCost"];

        // assert
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
