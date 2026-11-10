using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Elastic.Esql.Extensions;
using HotChocolate.Data.Filters;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data.ElasticSearch;

[Collection("Elastic Tests")]
public class ListFilterTests(ElasticsearchResource resource) : TestBase(resource)
{
    private static List<Item> Items(params (string Bar, string Qux)[] items)
        => [.. items.Select(i => new Item { Bar = i.Bar, Qux = i.Qux, Note = i.Bar, Tone = ToneOf(i.Bar), Hue = ToneOf(i.Bar) })];

    private static OperationFilterTests.Color ToneOf(string? bar)
        => bar?.StartsWith('B') == true ? OperationFilterTests.Color.Blue : OperationFilterTests.Color.Red;

    private static readonly (string Name, List<Item> Items)[] s_objects =
    [
        ("1", Items(("A1", "A1"), ("A2", "A2"), ("A3", "A3"))),
        ("2", Items(("B1", "B1"), ("B2", "B2"))),
        ("3", []),
        ("5", Items(("X1", "Y1"), ("X2", "Y2"))),
        ("6", Items(("A1", "Z"), ("A1", "A1"))),
        ("7", Items(("A:1 (x)", "a\"b"), ("and", "a b"))),
        ("8", Items(("a1", "a1"))),
        ("9", Items((null!, "N"))),
        ("10", [new Item { Bar = "D1", Qux = "D1" }]),
        ("11", [new Item { Bar = "D2", Qux = "D2", Details = new ItemDetails { Code = "a", Kind = "k" } }]),
        ("12", [new Item { Bar = "D3", Qux = "D3", Details = new ItemDetails { Kind = "k" } }])
    ];

    private static readonly StringDocument[] s_strings =
    [
        new() { Name = "1", Values = ["A1", "A2", "A3"] },
        new() { Name = "2", Values = ["B1", "B2"] },
        new() { Name = "3" },
        new() { Name = "5", Values = ["A1", "B1"] },
        new() { Name = "6", Values = ["A1", "A1"] },
        new() { Name = "7", Values = ["a.b", "a+b"] },
        new() { Name = "8", Values = ["a1"] },
        new() { Name = "9", Values = ["x|y", "A(1)"] },
        new() { Name = "10", Values = ["axb"] },
        new() { Name = "11", Values = [""] },
        new() { Name = "12", Values = ["A/1", "A-1"] }
    ];

    private static readonly (string Name, List<ReservedItem> Items)[] s_reserved =
    [
        ("1", [new ReservedItem { Bar = "x", Details = new ReservedDetails { Code = "a", Kind = "k" } }]),
        ("2", [new ReservedItem { Bar = "y" }]),
        ("3", [new ReservedItem { Bar = "y", Details = new ReservedDetails { Kind = "k" } }]),
        ("4", [])
    ];

    public static TheoryData<string> SingleField =>
    [
        """some: { bar: { eq: "A1" } }""",
        """none: { bar: { eq: "A1" } }""",
        "any: true",
        "any: false",
        """some: { bar: { in: ["A1", "B1"] } }""",
        """some: { bar: { startsWith: "A" } }""",
        """some: { bar: { endsWith: "1" } }""",
        """some: { bar: { contains: "1" } }""",
        """none: { bar: { startsWith: "A" } }""",
        """some: { bar: { eq: "A:1 (x)" } }""",
        """some: { qux: { eq: "a\"b" } }""",
        """some: { qux: { eq: "a b" } }""",
        """some: { bar: { eq: "and" } }""",
        """some: { bar: { contains: "1 (x" } }""",
        """some: { qux: { endsWith: " b" } }""",
        """some: { bar: { eq: "a1" } }""",
        "some: { bar: { in: [] } }",
        "none: { bar: { in: [] } }",
        "some: { note: { neq: null } }",
        "some: { tone: { eq: BLUE } }",
        "some: { hue: { eq: BLUE } }",
        "some: { tone: { in: [BLUE] } }",
        "some: { hue: { in: [BLUE] } }",
        """some: { details: { code: { eq: "a" } } }"""
    ];

    public static TheoryData<string> OneObject =>
    [
        """some: { bar: { neq: "A1" } }""",
        """all: { bar: { eq: "A1" } }""",
        """all: { bar: { neq: "A1" } }""",
        """some: { bar: { nin: ["A1", "B1"] } }""",
        """some: { bar: { nstartsWith: "A" } }""",
        """some: { bar: { ncontains: "1" } }""",
        """all: { bar: { startsWith: "A" } }""",
        """some: { bar: { eq: "A1" }, qux: { eq: "A1" } }""",
        """some: { bar: { eq: "A1" }, qux: { eq: "A2" } }""",
        """some: { bar: { eq: "X1" }, qux: { eq: "Y2" } }""",
        """some: { or: [{ bar: { eq: "B1" } }, { qux: { eq: "Y2" } }] }""",
        """some: { bar: { contains: "1", endsWith: "3" } }""",
        "all: { bar: { in: [] } }",
        "some: { note: { eq: null } }",
        """some: { details: { code: { neq: "a" } } }""",
        "some: { details: { code: { eq: null } } }",
        """some: { details: { code: { nstartsWith: "a" } } }""",
        """some: { note: { in: ["A1", null] } }""",
        """some: { note: { nin: ["A1", null] } }""",
        """some: { details: { code: { in: ["a", null] } } }"""
    ];

    public static TheoryData<string> Strings =>
    [
        """some: { nin: ["A1", "B1"] }""",
        """some: { ncontains: "1" }""",
        """some: { nstartsWith: "A" }""",
        """some: { nendsWith: "1" }""",
        """all: { startsWith: "A" }""",
        """all: { endsWith: "1" }""",
        """all: { contains: "1" }""",
        """all: { nstartsWith: "A" }""",
        """all: { ncontains: "1" }""",
        """all: { in: ["A1", "A2", "A3"] }""",
        """all: { nin: ["A1", "B1"] }""",
        """none: { nin: ["A1", "B1"] }""",
        """none: { ncontains: "1" }""",
        """some: { ncontains: "." }""",
        """all: { contains: "." }""",
        """some: { nin: ["a+b"] }""",
        """some: { nin: ["x|y"] }""",
        """all: { startsWith: "A(" }""",
        """some: { ncontains: "/" }""",
        """all: { contains: "-" }""",
        """all: { startsWith: "a" }""",
        """some: { nstartsWith: "A1" }""",
        "some: { nin: [] }",
        "all: { in: [] }",
        """some: { nin: ["A1", null] }""",
        """all: { in: ["A1", "A2", "A3", null] }""",
        """some: { in: ["A1", null] }""",
        """none: { in: ["A1", null] }"""
    ];

    [Theory]
    [MemberData(nameof(SingleField))]
    [MemberData(nameof(OneObject))]
    public async Task Filter_Should_MatchLinq_When_ListIsNested(string filter)
    {
        // arrange
        var data = s_objects.Select(o => new NestedDocument { Name = o.Name, Items = o.Items }).ToArray();
        await IndexDocuments(data);

        // act
        var expected = await ExecuteAsync(() => data.AsQueryable(), false, "items", filter);
        var actual = await ExecuteAsync(Query<NestedDocument>, true, "items", filter);

        // assert
        Assert.Equal(Names(expected), Names(actual));
    }

    [Theory]
    [MemberData(nameof(SingleField))]
    public async Task Filter_Should_MatchLinq_When_ListIsObjectMapped(string filter)
    {
        // arrange
        var data = s_objects.Select(o => new ObjectDocument { Name = o.Name, Items = o.Items }).ToArray();
        await IndexDocuments(data);

        // act
        var expected = await ExecuteAsync(() => data.AsQueryable(), false, "items", filter);
        var actual = await ExecuteAsync(Query<ObjectDocument>, true, "items", filter);

        // assert
        Assert.Equal(Names(expected), Names(actual));
    }

    [Theory]
    [MemberData(nameof(OneObject))]
    public async Task Filter_Should_ReportError_When_ListIsObjectMappedAndNeedsOneObject(string filter)
    {
        // arrange
        await IndexDocuments(s_objects.Select(o => new ObjectDocument { Name = o.Name, Items = o.Items }));

        // act
        var result = await ExecuteAsync(Query<ObjectDocument>, true, "items", filter);

        // assert
        Assert.Equal("ELASTICSEARCH_LIST_FILTER_REQUIRES_NESTED", result["errors"]?[0]?["extensions"]?["code"]?.ToString());
    }

    [Theory]
    [InlineData("any: true")]
    [InlineData("any: false")]
    public async Task Filter_Should_MatchLinq_When_NestedListHoldsEmptyElement(string filter)
    {
        // arrange
        NestedDocument[] data =
        [
            new() { Name = "1", Items = [new Item { Bar = null!, Qux = null! }] },
            new() { Name = "2" },
            new() { Name = "3", Items = Items(("A1", "A1")) }
        ];
        await IndexDocuments(data);

        // act
        var expected = await ExecuteAsync(() => data.AsQueryable(), false, "items", filter);
        var actual = await ExecuteAsync(Query<NestedDocument>, true, "items", filter);

        // assert
        Assert.Equal(Names(expected), Names(actual));
    }

    [Fact]
    public async Task Filter_Should_ReportError_When_NestedPrefixHasReservedCharacter()
    {
        // arrange
        await IndexDocuments(s_objects.Select(o => new NestedDocument { Name = o.Name, Items = o.Items }));

        // act
        var result = await ExecuteAsync(
            Query<NestedDocument>,
            true,
            "items",
            """some: { bar: { startsWith: "A:1" } }""");

        // assert
        Assert.Equal(
            "ELASTICSEARCH_LIST_FILTER_PATTERN_NOT_SUPPORTED",
            result["errors"]?[0]?["extensions"]?["code"]?.ToString());
    }

    [Theory]
    [MemberData(nameof(Strings))]
    public async Task Filter_Should_MatchLinq_When_ListHoldsStrings(string filter)
    {
        // arrange
        await IndexDocuments(s_strings);

        // act
        var expected = await ExecuteAsync(() => s_strings.AsQueryable(), false, "values", filter);
        var actual = await ExecuteAsync(Query<StringDocument>, true, "values", filter);

        // assert
        Assert.Equal(Names(expected), Names(actual));
    }

    [Theory]
    [InlineData("""some: { bar: { eq: "x" } }""")]
    [InlineData("""some: { bar: { neq: "x" } }""")]
    [InlineData("""some: { details: { code: { neq: "a" } } }""")]
    [InlineData("any: true")]
    public async Task Filter_Should_MatchLinq_When_NestedFieldNameHasReservedCharacter(string filter)
    {
        // arrange
        var data = s_reserved.Select(r => new ReservedNestedDocument { Name = r.Name, Items = r.Items }).ToArray();
        await IndexDocuments(data);

        // act
        var expected = await ExecuteAsync(() => data.AsQueryable(), false, "items", filter);
        var actual = await ExecuteAsync(Query<ReservedNestedDocument>, true, "items", filter);

        // assert
        Assert.Equal(Names(expected), Names(actual));
    }

    [Theory]
    [InlineData("""some: { bar: { eq: "x" } }""")]
    [InlineData("any: true")]
    public async Task Filter_Should_MatchLinq_When_ObjectFieldNameHasReservedCharacter(string filter)
    {
        // arrange
        var data = s_reserved.Select(r => new ReservedObjectDocument { Name = r.Name, Items = r.Items }).ToArray();
        await IndexDocuments(data);

        // act
        var expected = await ExecuteAsync(() => data.AsQueryable(), false, "items", filter);
        var actual = await ExecuteAsync(Query<ReservedObjectDocument>, true, "items", filter);

        // assert
        Assert.Equal(Names(expected), Names(actual));
    }

    [Theory]
    [InlineData("""some: { nstartsWith: "A" }""")]
    [InlineData("""all: { in: ["A1"] }""")]
    public async Task Filter_Should_MatchLinq_When_StringListNameHasReservedCharacter(string filter)
    {
        // arrange
        ReservedStringDocument[] data =
        [
            new() { Name = "1", Values = ["A1"] },
            new() { Name = "2", Values = ["B1"] }
        ];
        await IndexDocuments(data);

        // act
        var expected = await ExecuteAsync(() => data.AsQueryable(), false, "values", filter);
        var actual = await ExecuteAsync(Query<ReservedStringDocument>, true, "values", filter);

        // assert
        Assert.Equal(Names(expected), Names(actual));
    }

    [Theory]
    [InlineData(false, """some: { bar: { contains: "foo\nbar" } }""")]
    [InlineData(false, """some: { bar: { contains: "a\tb" } }""")]
    [InlineData(true, """some: { bar: { contains: "foo\nbar" } }""")]
    [InlineData(true, """some: { bar: { contains: "a\tb" } }""")]
    [InlineData(false, """some: { bar: { startsWith: "foo\nb" } }""")]
    [InlineData(true, """some: { bar: { endsWith: "\r\nz" } }""")]
    public async Task Filter_Should_MatchLinq_When_ValueHasWhitespace(bool nested, string filter)
    {
        // arrange
        var items = new[] { ("1", "foo\nbar"), ("2", "fooX"), ("3", "barn"), ("4", "a\tb"), ("5", "y\r\nz") };
        var objects = items.Select(i => new ObjectDocument { Name = i.Item1, Items = Items((i.Item2, "q")) }).ToArray();
        var nesteds = items.Select(i => new NestedDocument { Name = i.Item1, Items = Items((i.Item2, "q")) }).ToArray();

        // act
        JsonNode expected;
        JsonNode actual;

        if (nested)
        {
            await IndexDocuments(nesteds);
            expected = await ExecuteAsync(() => nesteds.AsQueryable(), false, "items", filter);
            actual = await ExecuteAsync(Query<NestedDocument>, true, "items", filter);
        }
        else
        {
            await IndexDocuments(objects);
            expected = await ExecuteAsync(() => objects.AsQueryable(), false, "items", filter);
            actual = await ExecuteAsync(Query<ObjectDocument>, true, "items", filter);
        }

        // assert
        Assert.Equal(Names(expected), Names(actual));
    }

    private IQueryable<T> Query<T>() where T : class => Client.Esql.CreateQuery<T>().From(Client.Index);

    internal static async Task<JsonNode> ExecuteAsync<T>(
        Func<IQueryable<T>> source,
        bool elasticSearch,
        string field,
        string filter)
        where T : class
    {
        var builder = new ServiceCollection().AddGraphQL();
        builder = elasticSearch ? builder.AddElasticSearchFiltering() : builder.AddFiltering();

        var executor = await builder
            .AddQueryType(d => d
                .Name("Query")
                .Field("test")
                .Type<ListType<ObjectType<T>>>()
                .Resolve(_ => elasticSearch ? source().AsElasticSearchExecutable() : source())
                .UseFiltering<FilterInputType<T>>())
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        var query = $$"""{ test(where: { {{field}}: { {{filter}} } }) { name } }""";
        var result = await executor.ExecuteAsync(query, TestContext.Current.CancellationToken);
        return JsonNode.Parse(result.ToJson())!;
    }

    internal static string Names(JsonNode result)
        => result["errors"]?[0]?["extensions"]?["exception"]?["message"]?.ToString()
            ?? result["errors"]?.ToJsonString()
            ?? string.Join(",", result["data"]!["test"]!.AsArray().Select(n => n!["name"]!.ToString()).Order(StringComparer.Ordinal));

    public class Item
    {
        public string Bar { get; set; } = "";

        public string Qux { get; set; } = "";

        public string? Note { get; set; }

        public OperationFilterTests.Color Tone { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public OperationFilterTests.Color Hue { get; set; }

        public ItemDetails? Details { get; set; }
    }

    public class ReservedItem
    {
        [JsonPropertyName("b ar")]
        public string? Bar { get; set; }

        [JsonPropertyName("de:tails")]
        public ReservedDetails? Details { get; set; }
    }

    public class ReservedDetails
    {
        [JsonPropertyName("co de")]
        public string? Code { get; set; }

        public string? Kind { get; set; }
    }

    public class ReservedNestedDocument
    {
        public string Name { get; set; } = "";

        [ElasticSearchNested]
        [JsonPropertyName("it ems")]
        public List<ReservedItem> Items { get; set; } = [];
    }

    public class ReservedObjectDocument
    {
        public string Name { get; set; } = "";

        [JsonPropertyName("it+ems")]
        public List<ReservedItem> Items { get; set; } = [];
    }

    public class ReservedStringDocument
    {
        public string Name { get; set; } = "";

        [JsonPropertyName("tag:values")]
        public List<string?> Values { get; set; } = [];
    }

    public class ItemDetails
    {
        public string? Code { get; set; }

        public string? Kind { get; set; }
    }

    public class NestedDocument
    {
        public string Name { get; set; } = "";

        [ElasticSearchNested]
        public List<Item> Items { get; set; } = [];
    }

    public class ObjectDocument
    {
        public string Name { get; set; } = "";

        public List<Item> Items { get; set; } = [];
    }

    public class StringDocument
    {
        public string Name { get; set; } = "";

        public List<string?> Values { get; set; } = [];
    }
}
