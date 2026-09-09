using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.CostAnalysis;

public sealed class DocumentShapeParityTests
{
    public static TheoryData<string, string> FixturePairs
        =>
        [
            ("c1-exclusive-types-fragments", "c1-exclusive-types"),
            ("c4-duplicate-response-name-fragments", "c4-duplicate-response-name")
        ];

    [Theory]
    [MemberData(nameof(FixturePairs))]
    public async Task Analyze_Should_ReportFixtureCost_When_NamedFragmentsAreRewritten(
        string namedFixtureId,
        string inlineFixtureId)
    {
        // arrange
        var namedFixture = LoadFixture(namedFixtureId);
        var inlineFixture = LoadFixture(inlineFixtureId);
        IOperation? capturedOperation = null;

        var requestExecutorBuilder =
            new ServiceCollection()
                .AddGraphQLServer()
                .AddDocumentFromString(StripBuiltInDirectiveDeclarations(namedFixture.Sdl))
                .ModifyCostOptions(o =>
                {
                    o.DefaultResolverCost = null;
                    o.ApplyCostDefaults = false;
                    o.DefaultListSize = ReadDefaultListSize(namedFixture.DefaultListSize);
                })
                .UseRequest(
                    next => async context =>
                    {
                        await next(context);

                        if (context.TryGetOperation(out var operation))
                        {
                            capturedOperation = operation;
                        }
                    },
                    key: "CaptureCompiledOperation",
                    before: WellKnownRequestMiddleware.CostAnalyzerMiddleware);

        AddStubResolvers(requestExecutorBuilder, namedFixtureId);
        var requestExecutor =
            await requestExecutorBuilder.BuildRequestExecutorAsync(
                cancellationToken: TestContext.Current.CancellationToken);

        // act
        var namedResult = await ExecuteAndReadCost(requestExecutor, namedFixture.Operation);
        var namedOperation = (Operation)(capturedOperation
            ?? throw new InvalidOperationException("The named-fragment operation was not compiled."));

        capturedOperation = null;
        var inlineResult = await ExecuteAndReadCost(requestExecutor, inlineFixture.Operation);

        var rawDocument = Utf8GraphQLParser.Parse(namedFixture.Operation);
        var rawDefinition = rawDocument.Definitions
            .OfType<OperationDefinitionNode>()
            .Single(definition => definition.Name?.Value == namedFixture.OperationName);
        var schemaSnapshot = requestExecutor.Schema.Services.GetRequiredService<CostSchemaSnapshot>();
        var rawPlan = CostPlanCompiler.Compile(
            schemaSnapshot,
            rawDocument,
            rawDefinition,
            CostAnalyses.Cost);
        var rewrittenPlan = CostPlanCompiler.Compile(
            schemaSnapshot,
            namedOperation.Document,
            namedOperation.Definition,
            CostAnalyses.Cost);
        var rawCost = ToCostValue(rawPlan.EvaluateStaticBound());
        var rewrittenCost = ToCostValue(rewrittenPlan.EvaluateStaticBound());
        var requestPlanCost = ToCostValue(GetCachedPlan(requestExecutor, namedOperation.Id).EvaluateStaticBound());
        var expected = new CostValue(
            namedFixture.Expected.TypeCost,
            namedFixture.Expected.FieldCost);
        var inlineExpected = new CostValue(
            inlineFixture.Expected.TypeCost,
            inlineFixture.Expected.FieldCost);

        // assert
        Assert.All(
            new[] { inlineExpected, namedResult, inlineResult, rawCost, rewrittenCost, requestPlanCost },
            actual => Assert.Equal(expected, actual));

        await new Snapshot(postFix: namedFixtureId)
            .Add(rawDocument, "Raw Named-Fragment Document")
            .Add(namedOperation.Document, "Rewritten Operation.Document")
            .Add(
                new
                {
                    Expected = expected,
                    NamedRequest = namedResult,
                    InlineRequest = inlineResult,
                    RawCompile = rawCost,
                    RewrittenCompile = rewrittenCost
                },
                "Costs")
            .MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    private static async Task<CostValue> ExecuteAndReadCost(
        IRequestExecutor requestExecutor,
        string document)
    {
        var request = OperationRequestBuilder.New()
            .SetDocument(document)
            .ReportCost()
            .Build();
        var result = await requestExecutor.ExecuteAsync(
            request,
            TestContext.Current.CancellationToken);
        var operationCost =
            (IReadOnlyDictionary<string, object?>)result
                .ExpectOperationResult()
                .Extensions["operationCost"]!;

        return new CostValue(
            Convert.ToDouble(operationCost["typeCost"]),
            Convert.ToDouble(operationCost["fieldCost"]));
    }

    private static CostPlan GetCachedPlan(IRequestExecutor requestExecutor, string operationId)
    {
        var cache = requestExecutor.Schema.Services.GetRequiredService<CostPlanCache>();
        return cache.TryGetPlan(operationId, out var plan)
            ? plan
            : throw new InvalidOperationException($"No cost plan was cached for operation '{operationId}'.");
    }

    private static CostValue ToCostValue(CostEstimate estimate)
        => new(estimate.TypeCost, estimate.FieldCost);

    private static ParityFixture LoadFixture(string fixtureId)
    {
        var path = System.IO.Path.Combine("__resources__", "article", fixtureId + ".json");
        var fixture = JsonSerializer.Deserialize<ParityFixture>(
            File.ReadAllText(path),
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        return fixture
            ?? throw new InvalidOperationException($"Fixture '{path}' deserialized to null.");
    }

    private static double ReadDefaultListSize(JsonElement value)
        => value.ValueKind == JsonValueKind.String
            ? double.PositiveInfinity
            : value.GetDouble();

    private static void AddStubResolvers(IRequestExecutorBuilder builder, string fixtureId)
    {
        if (fixtureId.StartsWith("c1-", StringComparison.Ordinal))
        {
            builder
                .AddResolver("Query", "result", _ => default(object))
                .AddResolver("A", "a", _ => 0)
                .AddResolver("B", "b", _ => 0);
        }
        else
        {
            builder
                .AddResolver("Query", "result", _ => default(object))
                .AddResolver("A", "a", _ => 0);
        }
    }

    private static string StripBuiltInDirectiveDeclarations(string sdl)
        => string.Join(
            '\n',
            sdl.Split('\n')
                .Where(line =>
                    !line.StartsWith("directive @cost", StringComparison.Ordinal)
                    && !line.StartsWith("directive @listSize", StringComparison.Ordinal)));

    private sealed record ParityFixture(
        string Id,
        string Sdl,
        string Operation,
        string? OperationName,
        JsonElement DefaultListSize,
        ParityExpected Expected);

    private sealed record ParityExpected(double TypeCost, double FieldCost);

    private sealed record CostValue(double TypeCost, double FieldCost);
}
