using System.Text.Json.Nodes;
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
        => [.. items.Select(i => new Item { Bar = i.Bar, Qux = i.Qux })];

    private static readonly (string Name, List<Item> Items)[] s_objects =
    [
        ("1", Items(("A1", "A1"), ("A2", "A2"), ("A3", "A3"))),
        ("2", Items(("B1", "B1"), ("B2", "B2"))),
        ("3", []),
        ("5", Items(("X1", "Y1"), ("X2", "Y2"))),
        ("6", Items(("A1", "Z"), ("A1", "A1"))),
        ("7", Items(("A:1 (x)", "a\"b"), ("and", "a b"))),
        ("8", Items(("a1", "a1")))
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
        """some: { bar: { eq: "a1" } }"""
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
        """some: { bar: { contains: "1", endsWith: "3" } }"""
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
        """some: { nstartsWith: "A1" }"""
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

        public List<string> Values { get; set; } = [];
    }
}
