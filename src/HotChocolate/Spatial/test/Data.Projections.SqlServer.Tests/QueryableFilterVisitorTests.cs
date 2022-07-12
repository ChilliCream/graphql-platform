using System.Threading.Tasks;
using HotChocolate.Execution;
using NetTopologySuite.Geometries;
using Squadron;
using Xunit;

namespace HotChocolate.Data.Projections.Spatial;

public class QueryableProjectionVisitorTests
    : SchemaCache
    , IClassFixture<PostgreSqlResource<PostgisConfig>>
{
    private static readonly Polygon _truePolygon =
        new(new LinearRing(
            new[]
            {
                new Coordinate(0, 0),
                new Coordinate(0, 2),
                new Coordinate(2, 2),
                new Coordinate(2, 0),
                new Coordinate(0, 0)
            }));

    private static readonly Polygon _falsePolygon =
        new(new LinearRing(
            new[]
            {
                new Coordinate(0, 0),
                new Coordinate(0, -2),
                new Coordinate(-2, -2),
                new Coordinate(-2, 0),
                new Coordinate(0, 0)
            }));

    private static readonly Foo[] _fooEntities =
    {
        new() { Id = 1, Bar = _truePolygon },
        new() { Id = 2, Bar = _falsePolygon }
    };

    public QueryableProjectionVisitorTests(PostgreSqlResource<PostgisConfig> resource)
        : base(resource)
    {
    }

    [Fact]

    public async Task Create_Expression()
    {
        // arrange
        var tester = await CreateSchemaAsync(_fooEntities);

        // act
        // assert
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    @"{
                        root {
                            id
                            bar { coordinates }
                        }
                    }")
                .Create());

        res1.MatchSqlSnapshot("");
    }

    public class Foo
    {
        public int Id { get; set; }

        public Polygon Bar { get; set; } = null!;
    }
}
