using HotChocolate.Execution;
using HotChocolate.Types.Descriptors;
using NetTopologySuite.Geometries;

namespace HotChocolate.Types.Spatial;

public class GeoJsonLineStringTypeTests
{
    private readonly LineString _geom = new(
    [
        new Coordinate(30, 10),
        new Coordinate(10, 30),
        new Coordinate(40, 40),
    ]);

    [Fact]
    public async Task LineString_Execution_Output()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddConvention<INamingConventions, MockNamingConvention>()
            .BindRuntimeType<Coordinate, GeoJsonPositionType>()
            .AddType<GeoJsonLineStringType>()
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
    public async Task LineString_Execution_With_Fragments()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddConvention<INamingConventions, MockNamingConvention>()
            .AddSpatialTypes()
            .AddQueryType(d => d
                .Name("Query")
                .Field("test")
                .Type<GeoJsonLineStringType>()
                .Resolve(_geom))
            .Create();
        var executor = schema.MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            "{ test { ... on LineString { type coordinates bbox crs }}}");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public void LineString_Execution_Tests() =>
        SchemaBuilder.New()
            .AddConvention<INamingConventions, MockNamingConvention>()
            .BindRuntimeType<Coordinate, GeoJsonPositionType>()
            .AddType<GeoJsonLineStringType>()
            .AddQueryType(d => d
                .Name("Query")
                .Field("test")
                .Resolve(_geom))
            .Create()
            .MatchSnapshot();

    [Fact]
    public async Task LineString_Execution_With_CRS()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddConvention<INamingConventions, MockNamingConvention>()
            .BindRuntimeType<Coordinate, GeoJsonPositionType>()
            .AddType<GeoJsonLineStringType>()
            .AddQueryType(d => d
                .Name("Query")
                .Field("test")
                .Resolve(_geom))
            .Create();

        // act
        var executor = schema.MakeExecutable();
        var result = await executor.ExecuteAsync("{ test { crs }}");

        // assert
        result.MatchSnapshot();
    }
}
