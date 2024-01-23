using System.Threading.Tasks;
using CookieCrumble;
using HotChocolate.Execution;

namespace HotChocolate.Data.Filters;

public class FilteringAndPaging
{
    private static readonly Foo[] _fooEntities =
    [
        new() { Bar = true, },
        new() { Bar = false, },
    ];

    private readonly SchemaCache _cache = new();

    [Fact]
    public async Task Create_BooleanEqual_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<Foo, FooFilterInput>(_fooEntities, true);

        // act
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { eq: true}}){ nodes { bar } }}")
                .Create());

        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { eq: false}}){ nodes { bar }}}")
                .Create());

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

    public class FooFilterInput
        : FilterInputType<Foo>
    {
    }

    public class FooNullableFilterInput
        : FilterInputType<FooNullable>
    {
    }
}
