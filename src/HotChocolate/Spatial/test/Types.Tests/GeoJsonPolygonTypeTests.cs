using HotChocolate.Execution;
using HotChocolate.Types.Descriptors;
using NetTopologySuite.Geometries;

namespace HotChocolate.Types.Spatial;

public class GeoJsonPolygonTypeTests
{
    private readonly Polygon _geom = new(
        new LinearRing(
        [
            new Coordinate(30, 10),
            new Coordinate(40, 40),
            new Coordinate(20, 40),
            new Coordinate(10, 20),
            new Coordinate(30, 10)
        ]));

    [Fact]
    public async Task GetCoordinates_Should_ReturnExteriorRingFirst_When_PolygonHasHoles()
    {
        // arrange
        var polygon = new Polygon(
            new LinearRing(
            [
                new Coordinate(0, 0),
                new Coordinate(40, 0),
                new Coordinate(40, 40),
                new Coordinate(0, 40),
                new Coordinate(0, 0)
            ]),
            [
                new LinearRing(
                [
                    new Coordinate(5, 5),
                    new Coordinate(15, 5),
                    new Coordinate(15, 15),
                    new Coordinate(5, 15),
                    new Coordinate(5, 5)
                ]),
                new LinearRing(
                [
                    new Coordinate(20, 20),
                    new Coordinate(30, 20),
                    new Coordinate(30, 30),
                    new Coordinate(20, 30),
                    new Coordinate(20, 20)
                ])
            ]);

        var schema = SchemaBuilder.New()
            .AddConvention<INamingConventions, MockNamingConvention>()
            .BindRuntimeType<Coordinate, GeoJsonPositionType>()
            .AddType<GeoJsonPolygonType>()
            .AddQueryType(
                d => d
                    .Name("Query")
                    .Field("test")
                    .Resolve(polygon))
            .Create();

        var executor = schema.MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            "{ test { type coordinates bbox crs }}",
            TestContext.Current.CancellationToken);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Polygon_Execution_Output()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddConvention<INamingConventions, MockNamingConvention>()
            .BindRuntimeType<Coordinate, GeoJsonPositionType>()
            .AddType<GeoJsonPolygonType>()
            .AddQueryType(
                d => d
                    .Name("Query")
                    .Field("test")
                    .Resolve(_geom))
            .Create();

        var executor = schema.MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            "{ test { type coordinates bbox crs }}",
            TestContext.Current.CancellationToken);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Polygon_Execution_With_Fragments()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddConvention<INamingConventions, MockNamingConvention>()
            .AddSpatialTypes()
            .AddQueryType(
                d => d
                    .Name("Query")
                    .Field("test")
                    .Type<GeoJsonPolygonType>()
                    .Resolve(_geom))
            .Create();
        var executor = schema.MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            "{ test { ... on Polygon { type coordinates bbox crs }}}",
            TestContext.Current.CancellationToken);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public void Polygon_Execution_Tests()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddConvention<INamingConventions, MockNamingConvention>()
            .BindRuntimeType<Coordinate, GeoJsonPositionType>()
            .AddType<GeoJsonPolygonType>()
            .AddQueryType(d => d
                .Name("Query")
                .Field("test")
                .Resolve(_geom))
            .Create();

        // assert
        schema.MatchSnapshot();
    }
}
