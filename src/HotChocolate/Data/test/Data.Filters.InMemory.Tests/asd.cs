using System.Threading.Tasks;
using HotChocolate.Execution;
using Snapshooter;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Data.Filters;

public class QueryableFilterCombinatorTests
{
    private static readonly Foo[] _fooEntities = { new() { Bar = true }, new() { Bar = false } };

    private readonly SchemaCache _cache = new();

    [Fact]
    public async Task Create_Empty_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<Foo, FooFilterInput>(_fooEntities);

        // act
        // assert
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(where: { }){ bar }}")
                .Create());

        res1.ToJson().MatchSnapshot(new SnapshotNameExtension("true"));
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
