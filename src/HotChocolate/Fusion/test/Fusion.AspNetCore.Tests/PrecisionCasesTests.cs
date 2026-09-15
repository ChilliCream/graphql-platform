using System.Text.Json;
using System.Text.Json.Serialization;
using HotChocolate.Transport;
using HotChocolate.Transport.Http;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

/// <summary>
/// Runs the IBM cost-precision fixtures against a single-source Fusion gateway.
/// Each request validates cost against the fixture's expected type and field values.
/// </summary>
public class PrecisionCasesTests : FusionTestBase
{
    private const string CostHeader = "GraphQL-Cost";
    private const string ValidateCost = "validate";

    public static TheoryData<string> FixturePaths => PrecisionCaseFixture.DiscoverPaths();

    [Theory]
    [MemberData(nameof(FixturePaths))]
    public async Task Fixture_Should_ReportExpectedCost_When_Validated(string path)
    {
        // arrange
        var fixture = PrecisionCaseFixture.Load(path);
        using var server = CreateSourceSchema(
            "A",
            builder =>
            {
                builder
                    .AddDocumentFromString(fixture.Sdl)
                    .AddResolverMocking();

                if (fixture.Id.StartsWith("c5-", StringComparison.Ordinal))
                {
                    builder.AddType(new AnyType("Text"));
                }
            });

        using var gateway = await CreateCompositeSchemaAsync(
            [("A", server)],
            // The fixture's DefaultListSize is now a composition-time setting: absence
            // (unbounded) is expressed as `null`, everything else as its integer value.
            defaultListSize: double.IsPositiveInfinity(fixture.DefaultListSize)
                ? null
                : (int)fixture.DefaultListSize);

        var request = new OperationRequest(
            fixture.Operation,
            operationName: fixture.OperationName,
            variables: fixture.Variables);

        var httpRequest = new GraphQLHttpRequest(request, new Uri("http://localhost:5000/graphql"))
        {
            OnMessageCreated = (_, message, _) => message.Headers.Add(CostHeader, ValidateCost)
        };

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        using var response = await client.SendAsync(httpRequest, TestContext.Current.CancellationToken);

        // assert
        await AssertAndMatchSnapshotAsync(
            gateway,
            request,
            response,
            results =>
            {
                var operationCost = Assert.Single(results).Extensions.GetProperty("operationCost");
                Assert.Equal(fixture.Expected.TypeCost, operationCost.GetProperty("typeCost").GetDouble());
                Assert.Equal(fixture.Expected.FieldCost, operationCost.GetProperty("fieldCost").GetDouble());
            },
            postFix: fixture.Id);
    }

    /// <summary>
    /// Contains the serialized inputs and expected output for one precision fixture.
    /// </summary>
    private sealed record PrecisionCaseFixtureData(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("sdl")] string Sdl,
        [property: JsonPropertyName("operation")] string Operation,
        [property: JsonPropertyName("operationName")] string? OperationName,
        [property: JsonPropertyName("variables")] JsonElement? Variables,
        [property: JsonPropertyName("defaultListSize")] JsonElement DefaultListSizeValue,
        [property: JsonPropertyName("expected")] PrecisionCaseExpected Expected);

    /// <summary>
    /// The expected typeCost/fieldCost pair of a <see cref="PrecisionCaseFixture"/>.
    /// </summary>
    private sealed record PrecisionCaseExpected(
        [property: JsonPropertyName("typeCost")] double TypeCost,
        [property: JsonPropertyName("fieldCost")] double FieldCost);

    /// <summary>
    /// Provides request-ready values for one precision fixture.
    /// </summary>
    private sealed class PrecisionCaseFixture
    {
        private const string ResourcesDirectoryName = "__resources__";
        private const string PrecisionDirectoryName = "precision";

        private PrecisionCaseFixture(
            string id,
            string sdl,
            string operation,
            string? operationName,
            IReadOnlyDictionary<string, object?>? variables,
            double defaultListSize,
            PrecisionCaseExpected expected)
        {
            Id = id;
            Sdl = sdl;
            Operation = operation;
            OperationName = operationName;
            Variables = variables;
            DefaultListSize = defaultListSize;
            Expected = expected;
        }

        public string Id { get; }

        public string Sdl { get; }

        public string Operation { get; }

        public string? OperationName { get; }

        public IReadOnlyDictionary<string, object?>? Variables { get; }

        public double DefaultListSize { get; }

        public PrecisionCaseExpected Expected { get; }

        public static TheoryData<string> DiscoverPaths()
        {
            var data = new TheoryData<string>();
            var directory = System.IO.Path.Combine(ResourcesDirectoryName, PrecisionDirectoryName);

            foreach (var path in Directory
                .EnumerateFiles(directory, "*.json")
                .OrderBy(path => path, StringComparer.Ordinal))
            {
                data.Add(path);
            }

            return data;
        }

        public static PrecisionCaseFixture Load(string path)
        {
            var json = File.ReadAllText(path);
            var data = JsonSerializer.Deserialize<PrecisionCaseFixtureData>(json)
                ?? throw new InvalidOperationException($"Fixture '{path}' deserialized to null.");

            return new PrecisionCaseFixture(
                data.Id,
                data.Sdl,
                data.Operation,
                data.OperationName,
                ToVariables(data.Variables),
                ToDefaultListSize(data.DefaultListSizeValue),
                data.Expected);
        }

        private static IReadOnlyDictionary<string, object?>? ToVariables(JsonElement? variables)
            => variables is null
                ? null
                : JsonSerializer.Deserialize<Dictionary<string, object?>>(variables.Value.GetRawText());

        private static double ToDefaultListSize(JsonElement value)
            => value.ValueKind == JsonValueKind.String
                ? double.PositiveInfinity
                : value.GetDouble();
    }
}
