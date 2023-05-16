using CookieCrumble;
using HotChocolate.Data.Neo4J;
using HotChocolate.Data.Neo4J.Testing;
using HotChocolate.Execution;

namespace HotChocolate.Data;

[Collection(Neo4JDatabaseCollectionFixture.DefinitionName)]
public class Neo4JRelationshipProjectionTests : IClassFixture<Neo4JFixture>
{
    private readonly Neo4JDatabase _database;
    private readonly Neo4JFixture _fixture;

    public Neo4JRelationshipProjectionTests(Neo4JDatabase database, Neo4JFixture fixture)
    {
        _database = database;
        _fixture = fixture;
    }

    private const string FooEntitiesCypher =
        "CREATE (:FooRel {BarBool: true, BarString: 'a', BarInt: 1, BarDouble: 1.5})-" +
        "[:RELATED_TO]->(:Bar {Name: 'b', Number: 2})<-[:RELATED_FROM]-" +
        "(:Baz {Name: 'c', Number: 3})";

    [Fact]
    public async Task Relationship_Projection_OneRelationshipReturnOneProperty()
    {
        // arrange
        var tester = await _fixture.Arrange<FooRel>(_database, FooEntitiesCypher);

        // act
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    @"
                        {
                            root {
                                barBool
                                barString
                                bars
                                {
                                    name
                                }
                            }
                        }
                        ")
                .Create());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1)
            .MatchAsync();
    }

    [Fact]
    public async Task Relationship_Projection_TwoRelationshipReturnOneProperty()
    {
        // arrange
        var tester = await _fixture.Arrange<FooRel>(_database, FooEntitiesCypher);

        // act

        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    @"{
                        root {
                            barBool
                            barString
                            bars
                            {
                                name
                                number
                                bazs
                                {
                                    name
                                }
                            }
                        }
                    }")
                .Create());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1)
            .MatchAsync();
    }

    public class FooRel
    {
        public bool BarBool { get; set; }

        public string BarString { get; set; } = string.Empty;

        public int BarInt { get; set; }

        public double BarDouble { get; set; }

        [Neo4JRelationship("RELATED_TO")]
        public List<Bar> Bars { get; set; } = default!;
    }

    public class Bar
    {
        public string Name { get; set; } = null!;

        public int Number { get; set; }

        [Neo4JRelationship("RELATED_FROM", RelationshipDirection.Incoming)]
        public List<Baz> Bazs { get; set; } = default!;
    }

    public class Baz
    {
        public string Name { get; set; } = null!;

        public int Number { get; set; } = default!;
    }
}
