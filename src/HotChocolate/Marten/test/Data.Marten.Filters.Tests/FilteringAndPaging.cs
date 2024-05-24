using CookieCrumble;
using HotChocolate.Data.Filters;
using HotChocolate.Execution;

namespace HotChocolate.Data;

[Collection(SchemaCacheCollectionFixture.DefinitionName)]
public class FilteringAndPaging(SchemaCache cache)
{
    private static readonly Foo[] _fooEntities = [new() { Bar = true, }, new() { Bar = false, },];

    [Fact]
    public async Task Create_BooleanEqual_Expression()
    {
        // arrange
        var tester = cache.CreateSchema<Foo, FooFilterInput>(_fooEntities, true);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { eq: true}}){ nodes { bar } }}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { eq: false}}){ nodes { bar }}}")
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
