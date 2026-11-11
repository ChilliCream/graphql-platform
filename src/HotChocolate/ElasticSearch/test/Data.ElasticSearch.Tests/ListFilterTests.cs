using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
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
        ("12", [new Item { Bar = "D3", Qux = "D3", Details = new ItemDetails { Kind = "k" } }]),
        ("13", [new Item { Bar = "D4", Qux = "D4", Details = new ItemDetails { Shipping = new() { City = "c" } } }])
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
        ("1", [new ReservedItem { Bar = "x", Dotted = "x", Details = new ReservedDetails { Code = "a", Kind = "k" } }]),
        ("2", [new ReservedItem { Bar = "y", Dotted = "y" }]),
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
    [InlineData("""some: { dotted: { neq: "x" } }""")]
    [InlineData("some: { dotted: { eq: null } }")]
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

    [Theory]
    [InlineData(false, "some: { tone: { eq: BLUE } }")]
    [InlineData(false, "some: { tone: { in: [BLUE, GREEN] } }")]
    [InlineData(false, "some: { hue: { eq: BLUE } }")]
    [InlineData(true, "some: { tone: { eq: BLUE } }")]
    [InlineData(true, "some: { tone: { neq: BLUE } }")]
    [InlineData(true, "some: { tone: { in: [BLUE, GREEN] } }")]
    [InlineData(true, "all: { tone: { nin: [RED] } }")]
    [InlineData(true, "some: { hue: { eq: BLUE } }")]
    public async Task Filter_Should_MatchLinq_When_OptionsWriteEnumsAsText(bool nested, string filter)
    {
        // arrange
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };

        // act
        var (expected, actual) = await ExecuteWithOptionsAsync(nested, filter, options);

        // assert
        Assert.Equal(Names(expected), Names(actual));
    }

    [Theory]
    [InlineData(false, """some: { bar: { eq: "B1" } }""")]
    [InlineData(false, """some: { details: { code: { eq: "a" } } }""")]
    [InlineData(true, """some: { bar: { eq: "A1" }, qux: { eq: "Z" } }""")]
    [InlineData(true, """some: { details: { shipping: { city: { eq: "c" } } } }""")]
    [InlineData(true, """none: { note: { eq: null } }""")]
    public async Task Filter_Should_MatchLinq_When_OptionsNameFieldsWithAnotherPolicy(bool nested, string filter)
    {
        // arrange
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseUpper,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver()
        };

        // act
        var (expected, actual) = await ExecuteWithOptionsAsync(nested, filter, options);

        // assert
        Assert.Equal(Names(expected), Names(actual));
    }

    [Theory]
    [InlineData(false, "items", """some: { bar: { eq: "Mario" } }""")]
    [InlineData(false, "items", """some: { bar: { eq: "mario" } }""")]
    [InlineData(false, "items", """some: { bar: { in: ["Mario", "Luigi"] } }""")]
    [InlineData(false, "items", """some: { bar: { contains: "Ros" } }""")]
    [InlineData(false, "items", """some: { bar: { startsWith: "Mario R" } }""")]
    [InlineData(false, "items", """some: { bar: { endsWith: "rossi" } }""")]
    [InlineData(false, "values", """some: { eq: "Mario" }""")]
    [InlineData(false, "values", """some: { neq: "Mario" }""")]
    [InlineData(false, "values", """all: { eq: "Mario" }""")]
    [InlineData(false, "values", """none: { eq: "mario rossi" }""")]
    [InlineData(false, "values", """some: { in: ["mario", "Luigi"] }""")]
    [InlineData(false, "values", """some: { startsWith: "Mario R" }""")]
    [InlineData(false, "values", """some: { ncontains: "Ros" }""")]
    [InlineData(false, "values", """none: { contains: "Ros" }""")]
    [InlineData(false, "values", """all: { endsWith: "i" }""")]
    [InlineData(true, "items", """some: { bar: { eq: "Mario" } }""")]
    [InlineData(true, "items", """some: { bar: { neq: "Mario" } }""")]
    [InlineData(true, "items", """some: { bar: { in: ["Mario", "Luigi"] } }""")]
    [InlineData(true, "items", """some: { bar: { contains: "Ros" } }""")]
    [InlineData(true, "items", """all: { bar: { endsWith: "rossi" } }""")]
    [InlineData(true, "items", """none: { bar: { eq: null } }""")]
    public async Task Filter_Should_MatchLinq_When_TextFieldHasKeywordSubfield(bool nested, string field, string filter)
    {
        // arrange
        string[] values = ["Mario", "mario rossi", "Mario Rossi", "Luigi"];
        var items = values.Select(v => new List<TextItem> { new() { Bar = v } }).Append(new List<TextItem> { new() }).ToArray();

        // act
        JsonNode expected;
        JsonNode actual;

        if (nested)
        {
            var data = items.Select((i, n) => new TextNestedDocument { Name = $"{n + 1}", Items = i }).ToArray();
            await IndexDocuments(data);
            expected = await ExecuteAsync(() => data.AsQueryable(), false, field, filter);
            actual = await ExecuteAsync(Query<TextNestedDocument>, true, field, filter);
        }
        else
        {
            var data = items.Select((i, n) => new TextObjectDocument
            {
                Name = $"{n + 1}",
                Items = i,
                Values = [.. i.Select(x => x.Bar).OfType<string>()]
            }).ToArray();
            await IndexDocuments(data);
            expected = await ExecuteAsync(() => data.AsQueryable(), false, field, filter);
            actual = await ExecuteAsync(Query<TextObjectDocument>, true, field, filter);
        }

        // assert
        Assert.Equal(Names(expected), Names(actual));
    }

    public static TheoryData<string> Numbers =>
    [
        "some: { count: { eq: -5 } }",
        "some: { count: { gt: -6 } }",
        "some: { count: { lte: -5 } }",
        "some: { count: { in: [-5, 3] } }",
        "some: { price: { eq: -1.5 } }",
        "some: { price: { lt: -1 } }",
        "some: { duration: { eq: \"PT5M\" } }",
        "some: { duration: { gt: \"PT2M\" } }",
        "some: { duration: { in: [\"PT5M\", \"PT1H\"] } }",
        "some: { at: { gt: \"2024-01-02T00:00:00Z\" } }",
        "some: { at: { eq: \"2024-01-01T00:00:00Z\" } }",
        "some: { key: { eq: \"00000000-0000-0000-0000-000000000001\" } }"
    ];

    private static readonly List<NumberItem>[] s_numbers =
    [
        [
            new NumberItem
            {
                Count = -5,
                Price = -1.5,
                Duration = TimeSpan.FromMinutes(5),
                At = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                Key = new Guid("00000000-0000-0000-0000-000000000001")
            }
        ],
        [
            new NumberItem
            {
                Count = 3,
                Price = 2,
                Duration = TimeSpan.FromMinutes(1),
                At = new DateTime(2024, 1, 3, 0, 0, 0, DateTimeKind.Utc)
            }
        ],
        [
            new NumberItem
            {
                Count = -10,
                Price = -3,
                Duration = TimeSpan.FromHours(1),
                At = new DateTime(2024, 1, 5, 0, 0, 0, DateTimeKind.Utc)
            }
        ]
    ];

    [Theory]
    [MemberData(nameof(Numbers))]
    public async Task Filter_Should_MatchLinq_When_ObjectListValueIsNegativeOrHasColon(string filter)
    {
        // arrange
        var data = s_numbers.Select((i, n) => new NumberObjectDocument { Name = $"{n + 1}", Items = i }).ToArray();
        await IndexDocuments(data);

        // act
        var expected = await ExecuteAsync(() => data.AsQueryable(), false, "items", filter);
        var actual = await ExecuteAsync(Query<NumberObjectDocument>, true, "items", filter);

        // assert
        Assert.Equal(Names(expected), Names(actual));
    }

    [Theory]
    [MemberData(nameof(Numbers))]
    [InlineData("some: { count: { neq: -5 } }")]
    [InlineData("some: { price: { nin: [-1.5, 2] } }")]
    public async Task Filter_Should_MatchLinq_When_NestedListValueIsNegativeOrHasColon(string filter)
    {
        // arrange
        var data = s_numbers.Select((i, n) => new NumberNestedDocument { Name = $"{n + 1}", Items = i }).ToArray();
        await IndexDocuments(data);

        // act
        var expected = await ExecuteAsync(() => data.AsQueryable(), false, "items", filter);
        var actual = await ExecuteAsync(Query<NumberNestedDocument>, true, "items", filter);

        // assert
        Assert.Equal(Names(expected), Names(actual));
    }

    private async Task<(JsonNode Expected, JsonNode Actual)> ExecuteWithOptionsAsync(
        bool nested,
        string filter,
        JsonSerializerOptions options)
    {
        var client = CreateClient(options);
        JsonNode expected;
        JsonNode actual;

        if (nested)
        {
            var data = s_objects.Select(o => new NestedDocument { Name = o.Name, Items = o.Items }).ToArray();
            await IndexDocuments(data, options);
            expected = await ExecuteAsync(() => data.AsQueryable(), false, "items", filter);
            actual = await ExecuteAsync(
                () => client.CreateQuery<NestedDocument>().From(Client.Index), true, "items", filter, options);
        }
        else
        {
            var data = s_objects.Select(o => new ObjectDocument { Name = o.Name, Items = o.Items }).ToArray();
            await IndexDocuments(data, options);
            expected = await ExecuteAsync(() => data.AsQueryable(), false, "items", filter);
            actual = await ExecuteAsync(
                () => client.CreateQuery<ObjectDocument>().From(Client.Index), true, "items", filter, options);
        }

        return (expected, actual);
    }

    private IQueryable<T> Query<T>() where T : class => Client.Esql.CreateQuery<T>().From(Client.Index);

    internal static async Task<JsonNode> ExecuteAsync<T>(
        Func<IQueryable<T>> source,
        bool elasticSearch,
        string field,
        string filter,
        JsonSerializerOptions? options = null)
        where T : class
    {
        var builder = new ServiceCollection().AddGraphQL();
        builder = !elasticSearch ? builder.AddFiltering()
            : options is null ? builder.AddElasticSearchFiltering()
            : builder.AddElasticSearchFiltering(options);

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

        [JsonPropertyName("n.ote")]
        public string? Dotted { get; set; }
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

        public Address? Billing { get; set; }

        public Address? Shipping { get; set; }
    }

    public class Address
    {
        public string? City { get; set; }
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

    public class NumberItem
    {
        public int Count { get; set; }

        public double Price { get; set; }

        public TimeSpan Duration { get; set; }

        public DateTime At { get; set; }

        public Guid Key { get; set; }
    }

    public class NumberObjectDocument
    {
        public string Name { get; set; } = "";

        public List<NumberItem> Items { get; set; } = [];
    }

    public class NumberNestedDocument
    {
        public string Name { get; set; } = "";

        [ElasticSearchNested]
        public List<NumberItem> Items { get; set; } = [];
    }

    public class TextItem
    {
        [ElasticSearchKeyword]
        public string? Bar { get; set; }
    }

    public class TextObjectDocument
    {
        public string Name { get; set; } = "";

        public List<TextItem> Items { get; set; } = [];

        [ElasticSearchKeyword("raw")]
        public List<string> Values { get; set; } = [];
    }

    public class TextNestedDocument
    {
        public string Name { get; set; } = "";

        [ElasticSearchNested]
        public List<TextItem> Items { get; set; } = [];
    }
}
