using CookieCrumble;
using HotChocolate.Execution;

namespace HotChocolate.Data.Neo4J.Projections.Relationship;

[Collection("Database")]
public class Neo4JRelationshipProjectionTests
{
    private readonly Neo4JFixture _fixture;

    public Neo4JRelationshipProjectionTests(Neo4JFixture fixture)
    {
        _fixture = fixture;
    }

    private readonly string _fooEntitiesCypher =
        "CREATE (:FooRel {BarBool: true, BarString: 'a', BarInt: 1, BarDouble: 1.5})-" +
        "[:RELATED_TO]->(:Bar {Name: 'b', Number: 2})<-[:RELATED_FROM]-" +
        "(:Baz {Name: 'c', Number: 3})";

    public class FooRel
    {
        public bool BarBool { get; set; }

        public string BarString { get; set; } = string.Empty;

        public int BarInt { get; set; }

        public double BarDouble { get; set; }

        [Neo4JRelationship("RELATED_TO")]
        public List<Bar> Bars { get; set; }
    }

    public class Bar
    {
        public string Name { get; set; } = null!;

        public int Number { get; set; }

        [Neo4JRelationship("RELATED_FROM", RelationshipDirection.Incoming)]
        public List<Baz> Bazs { get; set; }
    }

    public class Baz
    {
        public string Name { get; set; } = null!;

        public int Number { get; set; }
    }

    [Fact]
    public async Task OneRelationshipReturnOneProperty()
    {
        // arrange
        var tester = await _fixture.GetOrCreateSchema<FooRel>(_fooEntitiesCypher);

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
        await SnapshotExtensions.Add(
                Snapshot
                    .Create(), res1)
            .MatchAsync();
    }

    [Fact]
    public async Task TwoRelationshipReturnOneProperty()
    {
        // arrange
        var tester = await _fixture.GetOrCreateSchema<FooRel>(_fooEntitiesCypher);

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
        await SnapshotExtensions.Add(
                Snapshot
                    .Create(), res1)
            .MatchAsync();
    }
}
