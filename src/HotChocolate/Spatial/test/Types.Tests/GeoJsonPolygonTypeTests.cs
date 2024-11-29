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
            new Coordinate(30, 10),
        ]));

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
            "{ test { type coordinates bbox crs }}");

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
            "{ test { ... on Polygon { type coordinates bbox crs }}}");

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
