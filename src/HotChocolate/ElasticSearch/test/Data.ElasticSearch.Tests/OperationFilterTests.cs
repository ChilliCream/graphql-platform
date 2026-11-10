using System.Text.Json.Serialization;
using Elastic.Esql.Extensions;

namespace HotChocolate.Data.ElasticSearch;

[Collection("Elastic Tests")]
public class OperationFilterTests(ElasticsearchResource resource) : TestBase(resource)
{
    private static readonly Document[] s_data =
    [
        new()
        {
            Name = "1", Title = "alpha", Count = 1, Total = 10, Price = 1.5, Flag = true,
            Color = Color.Red, Shade = Color.Red, When = new DateTime(2022, 12, 22, 15, 5, 22, DateTimeKind.Utc),
            OptionalTitle = "x", OptionalCount = 1, OptionalFlag = true, Inner = new Inner { Code = "a" },
            OptionalColor = Color.Red
        },
        new()
        {
            Name = "2", Title = "beta", Count = 2, Total = 20, Price = 2.5, Flag = false,
            Color = Color.Green, Shade = Color.Green, When = new DateTime(2022, 12, 23, 15, 5, 22, DateTimeKind.Utc),
            OptionalFlag = false, Inner = new Inner { Code = "b" }
        },
        new()
        {
            Name = "3", Title = "Alpha", Count = 3, Total = 30, Price = -1, Flag = true,
            Color = Color.Blue, Shade = Color.Blue, When = new DateTime(2022, 12, 24, 15, 5, 22, DateTimeKind.Utc),
            OptionalTitle = "y", OptionalCount = 3, OptionalColor = Color.Blue
        },
        new()
        {
            Name = "4", Title = "a*b?c", Count = 0, Total = 0, Price = 0, Flag = false,
            Color = Color.Red, Shade = Color.Red, When = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Inner = new Inner { Code = "a" }
        },
        new()
        {
            Name = "5", Title = "say \"hi\" 100%", Count = -5, Total = 5_000_000_000, Price = 2.5, Flag = true,
            Color = Color.Green, Shade = Color.Green, When = new DateTime(2021, 6, 1, 12, 0, 0, DateTimeKind.Utc),
            OptionalTitle = "", OptionalCount = 0, OptionalFlag = true
        }
    ];

    public static TheoryData<string, string> Operations =>
    new()
    {
        { "title", """eq: "alpha" """ },
        { "title", """neq: "alpha" """ },
        { "title", """contains: "ph" """ },
        { "title", """ncontains: "ph" """ },
        { "title", """in: ["alpha", "beta"] """ },
        { "title", """nin: ["alpha", "beta"] """ },
        { "title", """startsWith: "a" """ },
        { "title", """nstartsWith: "a" """ },
        { "title", """endsWith: "a" """ },
        { "title", """nendsWith: "a" """ },
        { "title", """contains: "*" """ },
        { "title", """startsWith: "a*" """ },
        { "title", """endsWith: "?c" """ },
        { "title", """contains: "\"hi\"" """ },
        { "title", """eq: "say \"hi\" 100%" """ },
        { "count", "eq: 1" },
        { "count", "neq: 1" },
        { "count", "gt: 1" },
        { "count", "ngt: 1" },
        { "count", "gte: 1" },
        { "count", "ngte: 1" },
        { "count", "lt: 1" },
        { "count", "nlt: 1" },
        { "count", "lte: 1" },
        { "count", "nlte: 1" },
        { "count", "in: [1, 3]" },
        { "count", "nin: [1, 3]" },
        { "total", "gt: 4000000000" },
        { "total", "in: [10, 5000000000]" },
        { "price", "eq: 2.5" },
        { "price", "gt: 0" },
        { "price", "nlte: 1.5" },
        { "price", "in: [1.5, -1]" },
        { "when", """gt: "2022-12-23T15:05:22Z" """ },
        { "when", """nlt: "2022-12-23T15:05:22Z" """ },
        { "when", """eq: "2022-12-23T15:05:22Z" """ },
        { "when", """in: ["2022-12-23T15:05:22Z", "2023-01-01T00:00:00Z"] """ },
        { "flag", "eq: true" },
        { "flag", "eq: false" },
        { "flag", "neq: true" },
        { "flag", "neq: false" },
        { "color", "eq: RED" },
        { "color", "neq: RED" },
        { "color", "in: [RED, BLUE]" },
        { "color", "nin: [RED, BLUE]" },
        { "shade", "eq: RED" },
        { "shade", "neq: RED" },
        { "shade", "in: [RED, BLUE]" },
        { "shade", "nin: [RED, BLUE]" },
        { "optionalTitle", "eq: null" },
        { "optionalTitle", "neq: null" },
        { "optionalTitle", """eq: "x" """ },
        { "optionalTitle", """neq: "x" """ },
        { "optionalTitle", """eq: "" """ },
        { "optionalTitle", """startsWith: "x" """ },
        { "optionalTitle", """nstartsWith: "x" """ },
        { "optionalTitle", """in: ["x", "y"] """ },
        { "optionalTitle", """nin: ["x", "y"] """ },
        { "optionalTitle", """in: ["x", null] """ },
        { "optionalTitle", """nin: ["x", null] """ },
        { "optionalCount", "eq: null" },
        { "optionalCount", "neq: null" },
        { "optionalCount", "eq: 1" },
        { "optionalCount", "neq: 1" },
        { "optionalCount", "gt: 0" },
        { "optionalCount", "ngt: 0" },
        { "optionalCount", "in: [1, 3]" },
        { "optionalCount", "nin: [1, 3]" },
        { "optionalCount", "in: [1, null]" },
        { "optionalCount", "nin: [1, null]" },
        { "optionalCount", "nin: [null]" },
        { "optionalColor", "in: [RED, null]" },
        { "optionalColor", "nin: [RED, null]" },
        { "optionalFlag", "eq: null" },
        { "optionalFlag", "neq: null" },
        { "optionalFlag", "eq: true" },
        { "optionalFlag", "neq: true" },
        { "inner", """code: { eq: "a" }""" },
        { "inner", """code: { neq: "a" }""" },
        { "inner", """code: { startsWith: "a" }""" }
    };

    [Theory]
    [MemberData(nameof(Operations))]
    public async Task Filter_Should_MatchLinq_When_OperationIsApplied(string field, string filter)
    {
        // arrange
        await IndexDocuments(s_data);

        // act
        var expected = await ListFilterTests.ExecuteAsync(() => s_data.AsQueryable(), false, field, filter);
        var actual = await ListFilterTests.ExecuteAsync(
            () => Client.Esql.CreateQuery<Document>().From(Client.Index),
            true,
            field,
            filter);

        // assert
        Assert.Equal(ListFilterTests.Names(expected), ListFilterTests.Names(actual));
    }

    public enum Color
    {
        Red,
        Green,
        Blue
    }

    public class Inner
    {
        public string Code { get; set; } = "";
    }

    public class Document
    {
        public string Name { get; set; } = "";

        public string Title { get; set; } = "";

        public int Count { get; set; }

        public long Total { get; set; }

        public double Price { get; set; }

        public bool Flag { get; set; }

        public Color Color { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public Color Shade { get; set; }

        public DateTime When { get; set; }

        public string? OptionalTitle { get; set; }

        public int? OptionalCount { get; set; }

        public bool? OptionalFlag { get; set; }

        public Color? OptionalColor { get; set; }

        public Inner? Inner { get; set; }
    }
}
