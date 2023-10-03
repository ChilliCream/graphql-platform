using CookieCrumble;
using HotChocolate.Data.Neo4J.Testing;
using HotChocolate.Execution;

namespace HotChocolate.Data;

[Collection(Neo4JDatabaseCollectionFixture.DefinitionName)]
public class Neo4JOffsetPagingTests : IClassFixture<Neo4JFixture>
{
    private readonly Neo4JDatabase _database;
    private readonly Neo4JFixture _fixture;

    public Neo4JOffsetPagingTests(Neo4JDatabase database, Neo4JFixture fixture)
    {
        _database = database;
        _fixture = fixture;
    }

    private const string FooEntitiesCypher = @"
            CREATE
                (:Foo {Bar: 'a'}),
                (:Foo {Bar: 'b'}),
                (:Foo {Bar: 'd'}),
                (:Foo {Bar: 'e'}),
                (:Foo {Bar: 'f'})";

    [Fact]
    public async Task OffsetPaging_SchemaSnapshot()
    {
        // arrange
        var tester = await _fixture.Arrange<Foo>(_database, FooEntitiesCypher);
        tester.Schema.MatchSnapshot();
    }

    [Fact]
    public async Task Simple_StringList_Default_Items()
    {
        // arrange
        var tester = await _fixture.Arrange<Foo>(_database, FooEntitiesCypher);

        // act
        // assert
        var res1 = await tester.ExecuteAsync(
            @"{
                root {
                    items {
                        bar
                    }
                    pageInfo {
                        hasNextPage
                        hasPreviousPage
                    }
                }
            }");

        await SnapshotExtensions.AddResult(
                Snapshot
                    .Create(), res1)
            .MatchAsync();
    }

    [Fact]
    public async Task Simple_StringList_Take_2()
    {
        // arrange
        var tester = await _fixture.Arrange<Foo>(_database, FooEntitiesCypher);

        //act
        var result = await tester.ExecuteAsync(
            @"{
                root(take: 2) {
                    items {
                        bar
                    }
                    pageInfo {
                        hasNextPage
                        hasPreviousPage
                    }
                }
            }");

        // assert
        await SnapshotExtensions.AddResult(
                Snapshot
                    .Create(), result)
            .MatchAsync();
    }

    [Fact]
    public async Task Simple_StringList_Take_2_After()
    {
        // arrange
        var tester = await _fixture.Arrange<Foo>(_database, FooEntitiesCypher);

        // act
        var result = await tester.ExecuteAsync(
            @"{
                root(take: 2 skip: 2) {
                    items {
                        bar
                    }
                    pageInfo {
                        hasNextPage
                        hasPreviousPage
                    }
                }
            }");

        // assert
        await Snapshot.Create()
            .AddResult(result)
            .MatchAsync();
    }

    private sealed class Foo
    {
        public string Bar { get; set; } = default!;
    }
}
