using CookieCrumble;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Neo4J.Testing;
using HotChocolate.Execution;

namespace HotChocolate.Data.Neo4J.Filtering;

[Collection(Neo4JDatabaseCollectionFixture.DefinitionName)]
public class Neo4JListFilterTests : IClassFixture<Neo4JFixture>
{
    private readonly Neo4JDatabase _database;
    private readonly Neo4JFixture _fixture;

    public Neo4JListFilterTests(Neo4JDatabase database, Neo4JFixture fixture)
    {
        _database = database;
        _fixture = fixture;
    }

    private const string FooEntitiesCypher = @"
        CREATE (a:Foo {BarString: 'a'})-[:RELATED_FOO]->(:FooNested {Bar: 'a'})-[:RELATED_BAR]->(:BarNested {Foo: 'a'}),
                (a)-[:RELATED_FOO]->(:FooNested {Bar: 'a'})-[:RELATED_BAR]->(:BarNested {Foo: 'a'}),
                (a)-[:RELATED_FOO]->(:FooNested {Bar: 'a'})-[:RELATED_BAR]->(:BarNested {Foo: 'a'}),
                (b:Foo {BarString: 'b'})-[:RELATED_FOO]->(:FooNested {Bar: 'c'}),
                (b)-[:RELATED_FOO]->(:FooNested {Bar: 'a'}),
                (b)-[:RELATED_FOO]->(:FooNested {Bar: 'a'}),
                (c:Foo {BarString: 'c'})-[:RELATED_FOO]->(:FooNested {Bar: 'a'}),
                (c)-[:RELATED_FOO]->(:FooNested {Bar: 'd'}),
                (c)-[:RELATED_FOO]->(:FooNested {Bar: 'b'}),
                (d:Foo {BarString: 'd'})-[:RELATED_FOO]->(:FooNested {Bar: 'c'}),
                (d)-[:RELATED_FOO]->(:FooNested {Bar: 'd'}),
                (d)-[:RELATED_FOO]->(:FooNested {Bar: 'b'}),
                (e:Foo {BarString: 'e'})-[:RELATED_FOO]->(:FooNested),
                (e)-[:RELATED_FOO]->(:FooNested {Bar: 'd'}),
                (e)-[:RELATED_FOO]->(:FooNested {Bar: 'b'})";

    [Fact]
    public async Task Create_ArrayAllObjectStringEqual_Expression()
    {
        // arrange
        var tester = await _fixture.Arrange<Foo, FooFilterType>(
            _database, FooEntitiesCypher);

        // act
        // assert
        const string query1 =
            @"{
                root(where: {
                    barString: {
                        eq: ""a""
                    }
                    fooNested: {
                        all: {
                            bar: { eq: ""a"" }
                        }
                    }
                }){
                    barString
                    fooNested {
                        bar
                        barNested {
                            foo
                        }
                    }
                }
            }";

        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        // assert
        await Snapshot
            .Create().AddResult(res1, "all")
            .MatchAsync();
    }

    public class Foo
    {
        public string BarString { get; set; } = default!;

        [Neo4JRelationship("RELATED_FOO")]
        public List<FooNested> FooNested { get; set; } = default!;
    }

    public class FooNested
    {
        public string? Bar { get; set; }

        [Neo4JRelationship("RELATED_BAR")]
        public List<BarNested> BarNested { get; set; } = default!;
    }

    public class BarNested
    {
        public string? Foo { get; set; }
    }

    public class FooFilterType : FilterInputType<Foo>
    {
    }
}
