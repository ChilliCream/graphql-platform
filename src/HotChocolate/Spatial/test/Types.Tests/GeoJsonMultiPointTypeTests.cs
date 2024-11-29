using HotChocolate.Execution;
using HotChocolate.Types.Descriptors;
using NetTopologySuite.Geometries;

namespace HotChocolate.Types.Spatial;

public class GeoJsonMultiPointTypeTests
{
    private readonly MultiPoint _geom = new(
    [
        new Point(new Coordinate(10, 40)),
        new Point(new Coordinate(40, 30)),
        new Point(new Coordinate(20, 20)),
        new Point(new Coordinate(30, 10)),
    ]);

    [Fact]
    public async Task MultiPoint_Execution_Output()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddConvention<INamingConventions, MockNamingConvention>()
            .BindRuntimeType<Coordinate, GeoJsonPositionType>()
            .AddType<GeoJsonMultiPointType>()
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
    public async Task MultiPoint_Execution_With_Fragments()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddConvention<INamingConventions, MockNamingConvention>()
            .AddSpatialTypes()
            .AddQueryType(
                d => d
                    .Name("Query")
                    .Field("test")
                    .Type<GeoJsonMultiPointType>()
                    .Resolve(_geom))
            .Create();
        var executor = schema.MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            "{ test { ... on MultiPoint { type coordinates bbox crs }}}");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public void MultiPoint_Execution_Tests()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddConvention<INamingConventions, MockNamingConvention>()
            .BindRuntimeType<Coordinate, GeoJsonPositionType>()
            .AddType<GeoJsonMultiPointType>()
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
