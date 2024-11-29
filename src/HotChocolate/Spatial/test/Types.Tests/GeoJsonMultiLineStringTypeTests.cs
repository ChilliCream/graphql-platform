using HotChocolate.Execution;
using HotChocolate.Types.Descriptors;
using NetTopologySuite.Geometries;

namespace HotChocolate.Types.Spatial;

public class GeoJsonMultiLineStringTypeTests
{
    private readonly MultiLineString _geom = new(
    [
        new LineString(
        [
            new Coordinate(10, 10),
                new Coordinate(20, 20),
                new Coordinate(10, 40),
        ]),
            new LineString(
        [
            new Coordinate(40, 40),
                new Coordinate(30, 30),
                new Coordinate(40, 20),
                new Coordinate(30, 10),
        ]),
    ]);

    [Fact]
    public async Task MultiLineString_Execution_Output()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddConvention<INamingConventions, MockNamingConvention>()
            .BindRuntimeType<Coordinate, GeoJsonPositionType>()
            .AddType<GeoJsonMultiLineStringType>()
            .AddQueryType(d => d
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
    public async Task MultiLineString_Execution_With_Fragments()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddConvention<INamingConventions, MockNamingConvention>()
            .AddSpatialTypes()
            .AddQueryType(d => d
                .Name("Query")
                .Field("test")
                .Type<GeoJsonMultiLineStringType>()
                .Resolve(_geom))
            .Create();
        var executor = schema.MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            "{ test { ... on MultiLineString { type coordinates bbox crs }}}");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public void MultiLineString_Execution_Tests() =>
        SchemaBuilder.New()
            .AddConvention<INamingConventions, MockNamingConvention>()
            .BindRuntimeType<Coordinate, GeoJsonPositionType>()
            .AddType<GeoJsonMultiLineStringType>()
            .AddQueryType(d => d
                .Name("Query")
                .Field("test")
                .Resolve(_geom))
            .Create()
            .MatchSnapshot();
}
