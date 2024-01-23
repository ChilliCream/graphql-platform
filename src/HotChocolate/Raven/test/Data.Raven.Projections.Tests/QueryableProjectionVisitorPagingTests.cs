using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using CookieCrumble;
using HotChocolate.Execution;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Raven;

[Collection(SchemaCacheCollectionFixture.DefinitionName)]
public class QueryableProjectionVisitorPagingTests
{
    private static readonly Foo[] _fooEntities =
    [
        new() { Bar = true, Baz = "a", },
        new() { Bar = false, Baz = "b", },
    ];

    private static readonly FooNullable[] _fooNullableEntities =
    [
        new() { Bar = true, Baz = "a", },
        new() { Bar = null, Baz = null, },
        new() { Bar = false, Baz = "c", },
    ];

    private readonly SchemaCache _cache;

    public QueryableProjectionVisitorPagingTests(SchemaCache cache)
    {
        _cache = cache;
    }

    [Fact]
    public async Task Create_ProjectsTwoProperties_Nodes()
    {
        // arrange
        var tester = _cache.CreateSchema(
            _fooEntities,
            usePaging: true);

        // act
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root{ nodes { bar baz } }}")
                .Create());

        // assert
        await Snapshot
            .Create().AddResult(res1)
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ProjectsOneProperty_Nodes()
    {
        // arrange
        var tester = _cache.CreateSchema(
            _fooEntities,
            usePaging: true);

        // act
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root{ nodes { baz } }}")
                .Create());

        // assert
        await Snapshot
            .Create().AddResult(res1)
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ProjectsTwoProperties_Edges()
    {
        // arrange
        var tester = _cache.CreateSchema(
            _fooEntities,
            usePaging: true);

        // act
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root{ edges { node { bar baz }} }}")
                .Create());

        // assert
        await Snapshot
            .Create().AddResult(res1)
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ProjectsOneProperty_Edges()
    {
        // arrange
        var tester = _cache.CreateSchema(
            _fooEntities,
            usePaging: true);

        // act
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root{ edges { node { baz }} }}")
                .Create());

        // assert
        await Snapshot
            .Create().AddResult(res1)
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ProjectsTwoProperties_EdgesAndNodes()
    {
        // arrange
        var tester = _cache.CreateSchema(
            _fooEntities,
            usePaging: true);

        // act
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root{ nodes{ baz } edges { node { bar }} }}")
                .Create());

        // assert
        await Snapshot
            .Create().AddResult(res1)
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ProjectsOneProperty_EdgesAndNodesOverlap()
    {
        // arrange
        var tester = _cache.CreateSchema(
            _fooEntities,
            usePaging: true);

        // act
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root{ nodes{ baz } edges { node { baz }} }}")
                .Create());

        // assert
        await Snapshot
            .Create().AddResult(res1)
            .MatchAsync();
    }

    [Fact]
    public async Task CreateNullable_ProjectsTwoProperties_Nodes()
    {
        // arrange
        var tester = _cache.CreateSchema(
            _fooNullableEntities,
            usePaging: true);

        // act
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root{ nodes { bar baz } }}")
                .Create());

        // assert
        await Snapshot
            .Create().AddResult(res1)
            .MatchAsync();
    }

    [Fact]
    public async Task CreateNullable_ProjectsOneProperty_Nodes()
    {
        // arrange
        var tester = _cache.CreateSchema(
            _fooNullableEntities,
            usePaging: true);

        // act
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root{ nodes { baz } }}")
                .Create());

        // assert
        await Snapshot
            .Create().AddResult(res1)
            .MatchAsync();
    }

    [Fact]
    public async Task CreateNullable_ProjectsTwoProperties_Edges()
    {
        // arrange
        var tester = _cache.CreateSchema(
            _fooNullableEntities,
            usePaging: true);

        // act
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root{ edges { node { bar baz }} }}")
                .Create());

        // assert
        await Snapshot
            .Create().AddResult(res1)
            .MatchAsync();
    }

    [Fact]
    public async Task CreateNullable_ProjectsOneProperty_Edges()
    {
        // arrange
        var tester = _cache.CreateSchema(
            _fooNullableEntities,
            usePaging: true);

        // act
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root{ edges { node { baz }} }}")
                .Create());

        // assert
        await Snapshot
            .Create().AddResult(res1)
            .MatchAsync();
    }

    [Fact]
    public async Task CreateNullable_ProjectsTwoProperties_EdgesAndNodes()
    {
        // arrange
        var tester = _cache.CreateSchema(
            _fooNullableEntities,
            usePaging: true);

        // act
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root{ nodes{ baz } edges { node { bar }} }}")
                .Create());

        // assert
        await Snapshot
            .Create().AddResult(res1)
            .MatchAsync();
    }

    [Fact]
    public async Task CreateNullable_ProjectsOneProperty_EdgesAndNodesOverlap()
    {
        // arrange
        var tester = _cache.CreateSchema(
            _fooNullableEntities,
            usePaging: true);

        // act
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root{ nodes{ baz } edges { node { baz }} }}")
                .Create());

        // assert
        await Snapshot
            .Create().AddResult(res1)
            .MatchAsync();
    }

    [Fact]
    public async Task Create_Projection_Should_Stop_When_UseProjectionEncountered()
    {
        // arrange
        var tester = _cache.CreateSchema(
            _fooEntities,
            usePaging: true);

        // act
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root{ nodes{ bar list { barBaz } } }}")
                .Create());

        // assert
        await Snapshot
            .Create().AddResult(res1)
            .MatchAsync();
    }

    [Fact]
    public async Task Create_Projection_Should_Stop_When_UsePagingEncountered()
    {
        // arrange
        var tester = _cache.CreateSchema(
            _fooEntities,
            usePaging: true);

        // act
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root{ nodes{ bar paging { nodes {barBaz }} } }}")
                .Create());

        // assert
        await Snapshot
            .Create().AddResult(res1)
            .MatchAsync();
    }

    [Fact]
    public async Task CreateOffsetPaging_ProjectsTwoProperties_Items_WithArgs()
    {
        // arrange
        var tester = _cache.CreateSchema(
            _fooEntities,
            useOffsetPaging: true);

        // act
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(take:10, skip:1){ items { bar baz } }}")
                .Create());

        // assert
        await Snapshot
            .Create().AddResult(res1)
            .MatchAsync();
    }

    [Fact]
    public async Task CreateOffsetPaging_ProjectsTwoProperties_Items()
    {
        // arrange
        var tester = _cache.CreateSchema(
            _fooEntities,
            useOffsetPaging: true);

        // act
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root{ items { bar baz } }}")
                .Create());

        // assert
        await Snapshot
            .Create().AddResult(res1)
            .MatchAsync();
    }

    [Fact]
    public async Task CreateOffsetPaging_ProjectsOneProperty_Items()
    {
        // arrange
        var tester = _cache.CreateSchema(
            _fooEntities,
            useOffsetPaging: true);

        // act
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root{ items { baz } }}")
                .Create());

        // assert
        await Snapshot
            .Create().AddResult(res1)
            .MatchAsync();
    }


    [Fact]
    public async Task CreateOffsetPagingNullable_ProjectsTwoProperties_Items()
    {
        // arrange
        var tester = _cache.CreateSchema(
            _fooNullableEntities,
            useOffsetPaging: true);

        // act
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root{ items { bar baz } }}")
                .Create());

        // assert
        await Snapshot
            .Create().AddResult(res1)
            .MatchAsync();
    }

    [Fact]
    public async Task CreateOffsetPagingNullable_ProjectsOneProperty_Items()
    {
        // arrange
        var tester = _cache.CreateSchema(
            _fooNullableEntities,
            useOffsetPaging: true);

        // act
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root{ items { baz } }}")
                .Create());

        // assert
        await Snapshot
            .Create().AddResult(res1)
            .MatchAsync();
    }

    [Fact]
    public async Task CreateOffsetPaging_Projection_Should_Stop_When_UseProjectionEncountered()
    {
        // arrange
        var tester = _cache.CreateSchema(
            _fooEntities,
            useOffsetPaging: true);

        // act
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root{ items{ bar list { barBaz } } }}")
                .Create());

        // assert
        await Snapshot
            .Create().AddResult(res1)
            .MatchAsync();
    }

    [Fact]
    public async Task CreateOffsetPaging_Projection_Should_Stop_When_UsePagingEncountered()
    {
        // arrange
        var tester = _cache.CreateSchema(
            _fooEntities,
            useOffsetPaging: true);

        // act
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root{ items{ bar paging { nodes {barBaz }} } }}")
                .Create());

        // assert
        await Snapshot
            .Create().AddResult(res1)
            .MatchAsync();
    }

    [Fact]
    public async Task CreateNullable_NodesAndEdgesWithAliases()
    {
        // arrange
        var tester = _cache.CreateSchema(
            _fooNullableEntities,
            usePaging: true);

        // act
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root{ b: nodes{ baz } a: edges { node { bar }} }}")
                .Create());

        // assert
        await Snapshot
            .Create().AddResult(res1)
            .MatchAsync();
    }

    public class Foo
    {
        public string? Id { get; set; }

        public bool Bar { get; set; }

        public string? Baz { get; set; }

        public string? Qux { get; set; }

        [UseProjection]
        [SubData]
        public List<Bar>? List { get; set; }

        [UsePaging]
        [UseProjection]
        [SubData]
        public List<Bar>? Paging { get; set; }
    }

    public class Bar
    {
        public string? Id { get; set; }

        public string? BarBaz { get; set; }

        public string? BarQux { get; set; }
    }

    public class FooNullable
    {
        public string? Id { get; set; }

        public bool? Bar { get; set; }

        public string? Baz { get; set; }
    }

    public class SubDataAttribute : ObjectFieldDescriptorAttribute
    {
        protected override void OnConfigure(
            IDescriptorContext context,
            IObjectFieldDescriptor descriptor,
            MemberInfo member)
        {
            descriptor.Resolve(
                new List<Bar>
                {
                    new() { BarBaz = "a_a", BarQux = "a_c", },
                    new() { BarBaz = "a_b", BarQux = "a_d", },
                });
        }
    }
}
