using CookieCrumble;
using HotChocolate.Execution;

namespace HotChocolate.Data.Filters;

public class QueryableFilterVisitorVariablesTests(SchemaCache cache) : IClassFixture<SchemaCache>
{
    private static readonly Foo[] _fooEntities =
    [
        new Foo { Bar = true, },
        new Foo { Bar = false, },
    ];

    [Fact]
    public async Task Create_BooleanEqual_Expression()
    {
        // arrange
        var tester = cache.CreateSchema<Foo, FooFilterInput>(_fooEntities);
        const string query =
            "query Test($where: Boolean){ root(where: {bar: { eq: $where}}){ bar}}";

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(query)
                .SetVariableValues(new Dictionary<string, object?> { { "where", true }, })
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(query)
                .SetVariableValues(new Dictionary<string, object?> { { "where", false }, })
                .Build());

        // assert
        await Snapshot
            .Create()
            .Add(res1, "true")
            .Add(res2, "false")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_BooleanEqual_Expression_NonNull()
    {
        // arrange
        var tester = cache.CreateSchema<Foo, FooFilterInput>(_fooEntities);
        const string query =
            "query Test($where: Boolean!){ root(where: {bar: { eq: $where}}){ bar}}";

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(query)
                .SetVariableValues(new Dictionary<string, object?> { { "where", true}, })
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(query)
                .SetVariableValues(new Dictionary<string, object?> { { "where", false}, })
                .Build());

        // assert
        await Snapshot
            .Create()
            .Add(res1, "true")
            .Add(res2, "false")
            .MatchAsync();
    }

    public class Foo
    {
        public int Id { get; set; }

        public bool Bar { get; set; }
    }

    public class FooNullable
    {
        public int Id { get; set; }

        public bool? Bar { get; set; }
    }

    public class FooFilterInput : FilterInputType<Foo>;

    public class FooNullableFilterInput : FilterInputType<FooNullable>;
}