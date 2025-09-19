using HotChocolate.Execution;
using HotChocolate.Types.Descriptors;
using NetTopologySuite.Geometries;

namespace HotChocolate.Types.Spatial;

public class GeoJsonPointTypeTests
{
    private readonly Point _geom = new(new Coordinate(30, 10));

    [Fact]
    public async Task Point_Execution_Output_Scalar()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddConvention<INamingConventions, MockNamingConvention>()
            .AddQueryType(d => d
                .Name("Query")
                .Field("test")
                .Type<GeometryType>()
                .Resolve(_geom))
            .Create();

        var executor = schema.MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            "{ test }");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Point_Execution_Output()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddConvention<INamingConventions, MockNamingConvention>()
            .BindRuntimeType<Coordinate, GeoJsonPositionType>()
            .AddType<GeoJsonPointType>()
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
    public async Task Point_Execution_With_Fragments()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddConvention<INamingConventions, MockNamingConvention>()
            .AddSpatialTypes()
            .AddQueryType(d => d
                .Name("Query")
                .Field("test")
                .Type<GeoJsonPointType>()
                .Resolve(_geom))
            .Create();
        var executor = schema.MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            "{ test { ... on Point { type coordinates bbox crs }}}");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public void Point_Execution_Tests()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddConvention<INamingConventions, MockNamingConvention>()
            .BindRuntimeType<Coordinate, GeoJsonPositionType>()
            .AddType<GeoJsonPointType>()
            .AddQueryType(d => d
                .Name("Query")
                .Field("test")
                .Resolve(_geom))
            .Create();

        // assert
        schema.MatchSnapshot();
    }
}
