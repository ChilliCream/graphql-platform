using HotChocolate.Execution;
using HotChocolate.Types.Descriptors;
using NetTopologySuite.Geometries;

namespace HotChocolate.Types.Spatial;

public class GeoJsonMultiPolygonTypeTests
{
    private readonly MultiPolygon _geom = new(
    [
        new Polygon(new LinearRing(
        [
            new Coordinate(30, 20),
            new Coordinate(45, 40),
            new Coordinate(10, 40),
            new Coordinate(30, 20)
        ])),
        new Polygon(new LinearRing(
        [
            new Coordinate(15, 5),
            new Coordinate(40, 10),
            new Coordinate(10, 20),
            new Coordinate(5, 15),
            new Coordinate(15, 5)
        ]))
    ]);

    [Fact]
    public async Task GetMultiPolygonCoordinates_Should_ReturnRingsPerPolygon_When_PolygonHasHoles()
    {
        // arrange
        var multiPolygon = new MultiPolygon(
        [
            new Polygon(
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
                        new Coordinate(10, 10),
                        new Coordinate(20, 10),
                        new Coordinate(20, 20),
                        new Coordinate(10, 20),
                        new Coordinate(10, 10)
                    ])
                ]),
            new Polygon(
                new LinearRing(
                [
                    new Coordinate(50, 50),
                    new Coordinate(60, 50),
                    new Coordinate(60, 60),
                    new Coordinate(50, 60),
                    new Coordinate(50, 50)
                ]))
        ]);

        var schema = SchemaBuilder.New()
            .AddConvention<INamingConventions, MockNamingConvention>()
            .BindRuntimeType<Coordinate, GeoJsonPositionType>()
            .AddType<GeoJsonMultiPolygonType>()
            .AddQueryType(
                d => d
                    .Name("Query")
                    .Field("test")
                    .Resolve(multiPolygon))
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
    public async Task MultiPolygon_Execution_Output()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddConvention<INamingConventions, MockNamingConvention>()
            .BindRuntimeType<Coordinate, GeoJsonPositionType>()
            .AddType<GeoJsonMultiPolygonType>()
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
    public async Task MultiPolygon_Execution_With_Fragments()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddConvention<INamingConventions, MockNamingConvention>()
            .AddSpatialTypes()
            .AddQueryType(
                d => d
                    .Name("Query")
                    .Field("test")
                    .Type<GeoJsonMultiPolygonType>()
                    .Resolve(_geom))
            .Create();
        var executor = schema.MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            "{ test { ... on GeoJSONMultiPolygonType { type coordinates bbox crs }}}",
            TestContext.Current.CancellationToken);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public void MultiPolygon_Execution_Tests()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddConvention<INamingConventions, MockNamingConvention>()
            .BindRuntimeType<Coordinate, GeoJsonPositionType>()
            .AddType<GeoJsonMultiPolygonType>()
            .AddQueryType(
                d => d
                    .Name("Query")
                    .Field("test")
                    .Resolve(_geom))
            .Create();

        // act
        // assert
        schema.MatchSnapshot();
    }
}
