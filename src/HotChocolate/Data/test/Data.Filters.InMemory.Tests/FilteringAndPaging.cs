using HotChocolate.Execution;

namespace HotChocolate.Data.Filters;

public class FilteringAndPaging
{
    private static readonly Foo[] s_fooEntities =
    [
        new() { Bar = true },
        new() { Bar = false }
    ];

    private readonly SchemaCache _cache = new();

    [Fact]
    public async Task Create_BooleanEqual_Expression()
    {
        // arrange
        var snapshot = new Snapshot();
        var tester = _cache.CreateSchema<Foo, FooFilterInput>(s_fooEntities, true);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { bar: { eq: true}}){ nodes { bar } }}")
                .Build());
        snapshot.Add(res1, "true");

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { bar: { eq: false}}){ nodes { bar }}}")
                .Build());
        snapshot.Add(res2, "true");

        // assert
        await snapshot.MatchAsync();
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

    public class FooFilterInput
        : FilterInputType<Foo>;

    public class FooNullableFilterInput
        : FilterInputType<FooNullable>;
}
