// ReSharper disable UnusedMember.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

using HotChocolate.Execution;

namespace HotChocolate.Data.Filters;

public class QueryableFilterCombinatorTests
{
    private static readonly Foo[] _fooEntities =
    [
        new Foo(bar: true),
        new Foo(bar: false),
    ];

    private readonly SchemaCache _cache = new();

    [Fact]
    public async Task Create_Empty_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<Foo, FooFilterInput>(_fooEntities);

        // act
        // assert
        var res1 = await tester.ExecuteAsync(
            """
            {
              root(where: { }) {
                bar
              }
            }
            """);

        res1.MatchSnapshot();
    }

    public class Foo
    {
        public Foo() { }

        public Foo(bool bar)
        {
            Bar = bar;
        }

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
