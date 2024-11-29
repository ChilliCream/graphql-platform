using HotChocolate.Configuration;
using HotChocolate.Execution;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace HotChocolate.Types.Spatial;

public class TransformationIntegrationTests
{
    private const string WKT4326 =
        "GEOGCS[\"WGS 84\",DATUM[\"WGS_1984\",SPHEROID[\"WGS 84\",6378137,298.257223563," +
        "AUTHORITY[\"EPSG\",\"7030\"]],AUTHORITY[\"EPSG\",\"6326\"]],PRIMEM[\"Greenwich\"," +
        "0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.0174532925199433,AUTHORITY[\"EPSG\"," +
        "\"9122\"]],AUTHORITY[\"EPSG\",\"4326\"]]";
    private const string WKT26918 =
        "PROJCS[\"NAD83 \\/ UTM zone 18N\",GEOGCS[\"NAD83\",DATUM[\"North_American_Datum_1983\"," +
        "SPHEROID[\"GRS 1980\",6378137,298.257222101,AUTHORITY[\"EPSG\",\"7019\"]],TOWGS84" +
        "[0,0,0,0,0,0,0],AUTHORITY[\"EPSG\",\"6269\"]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\"," +
        "\"8901\"]],UNIT[\"degree\",0.0174532925199433,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[" +
        "\"EPSG\",\"4269\"]],PROJECTION[\"Transverse_Mercator\"],PARAMETER[" +
        "\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",-75],PARAMETER[" +
        "\"scale_factor\",0.9996],PARAMETER[\"false_easting\",500000],PARAMETER[" +
        "\"false_northing\",0],UNIT[\"metre\",1,AUTHORITY[\"EPSG\",\"9001\"]],AXIS[\"Easting\"," +
        "EAST],AXIS[\"Northing\",NORTH],AUTHORITY[\"EPSG\",\"26918\"]]";

    [Fact]
    public void Execute_UnknownDefaultCRS()
    {
        // arrange
        var builder = SchemaBuilder.New()
            .AddQueryType<Query>()
            .AddSpatialTypes(x => x
                .DefaultSrid(4326)
                .AddCoordinateSystemFromString(26918, WKT26918))
            .TryAddTypeInterceptor<RoundTypeIntercetor>();

        // act
        var ex = Record.Exception(() => builder.Create());

        // assert
        Assert.IsType<SchemaException>(ex).MatchSnapshot();
    }

    [Fact]
    public async Task Execute_InputUnknownCRS_RaiseException()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType<Query>()
            .AddSpatialTypes(x => x
                .DefaultSrid(4326)
                .AddCoordinateSystemFromString(4326, WKT4326))
            .TryAddTypeInterceptor<RoundTypeIntercetor>()
            .Create();

        var executor = schema.MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            @"
                {
                    test(
                        arg: {
                            type: LineString,
                            crs: 26918,
                            coordinates: [[30, 10], [10, 30], [40, 40]]
                        }) {
                            type
                            crs
                            coordinates
                    }
                }");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Execute_OutputUnknownCRS_RaiseException()
    {
        // arrange
        var lineString = NtsGeometryServices.Instance
            .CreateGeometryFactory(4326)
            .CreateLineString(
            [
                new Coordinate(30, 10),
                    new Coordinate(10, 30),
                    new Coordinate(40, 40),
            ]);

        var schema = SchemaBuilder.New()
            .AddDocumentFromString(
                @"
                    type Query {
                        test: GeoJSONLineStringType
                    }
                ")
            .AddSpatialTypes(x => x
                .DefaultSrid(4326)
                .AddCoordinateSystemFromString(4326, WKT4326))
            .AddResolver("Query", "test", _ => lineString)
            .TryAddTypeInterceptor<RoundTypeIntercetor>()
            .Create();

        var executor = schema.MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            @"
                {
                    test(crs: 26918) {
                        type
                        crs
                        coordinates
                    }
                }");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Execute_CoordinateZM_RaiseException()
    {
        // arrange
        var lineString = NtsGeometryServices.Instance
            .CreateGeometryFactory(4326)
            .CreateLineString(
            [
                new Coordinate(30, 10),
                    new Coordinate(10, 30),
                    new CoordinateZM(10, 30, 12, 15),
                    new Coordinate(40, 40),
            ]);

        var schema = SchemaBuilder.New()
            .AddDocumentFromString(
                @"
                        type Query {
                            test: GeoJSONLineStringType
                        }
                    ")
            .AddSpatialTypes(x => x
                .DefaultSrid(4326)
                .AddCoordinateSystemFromString(4326, WKT4326)
                .AddCoordinateSystemFromString(26918, WKT26918))
            .AddResolver("Query", "test", _ => lineString)
            .TryAddTypeInterceptor<RoundTypeIntercetor>()
            .Create();

        var executor = schema.MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            @"
                {
                    test(crs: 26918) {
                        type
                        crs
                        coordinates
                    }
                }");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Execute_CoordinateM_RaiseException()
    {
        // arrange
        var lineString = NtsGeometryServices.Instance
            .CreateGeometryFactory(4326)
            .CreateLineString(
            [
                new Coordinate(30, 10),
                    new Coordinate(10, 30),
                    new CoordinateM(10, 30, 12),
                    new Coordinate(40, 40),
            ]);

        var schema = SchemaBuilder.New()
            .AddDocumentFromString(
                @"
                        type Query {
                            test: GeoJSONLineStringType
                        }
                    ")
            .AddSpatialTypes(x => x
                .DefaultSrid(4326)
                .AddCoordinateSystemFromString(4326, WKT4326)
                .AddCoordinateSystemFromString(26918, WKT26918))
            .AddResolver("Query", "test", _ => lineString)
            .TryAddTypeInterceptor<RoundTypeIntercetor>()
            .Create();

        var executor = schema.MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            @"
                {
                    test(crs: 26918) {
                        type
                        crs
                        coordinates
                    }
                }");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Execute_CoordinateZ_NotRaiseException()
    {
        // arrange
        var lineString = NtsGeometryServices.Instance
            .CreateGeometryFactory(4326)
            .CreateLineString(
            [
                new Coordinate(30, 10),
                    new Coordinate(10, 30),
                    new CoordinateZ(10, 30, 12),
                    new Coordinate(40, 40),
            ]);

        var schema = SchemaBuilder.New()
            .AddDocumentFromString(
                @"
                        type Query {
                            test: GeoJSONLineStringType
                        }
                    ")
            .AddSpatialTypes(x => x
                .DefaultSrid(4326)
                .AddCoordinateSystemFromString(4326, WKT4326)
                .AddCoordinateSystemFromString(26918, WKT26918))
            .AddResolver("Query", "test", _ => lineString)
            .TryAddTypeInterceptor<RoundTypeIntercetor>()
            .Create();

        var executor = schema.MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            @"
                {
                    test(crs: 26918) {
                        type
                        crs
                        coordinates
                    }
                }");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Execute_NoTransformer_NotTransformedInput()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType<Query>()
            .AddSpatialTypes()
            .TryAddTypeInterceptor<RoundTypeIntercetor>()
            .Create();

        var executor = schema.MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            @"
                {
                    test(
                        arg: {
                            type: LineString,
                            crs: 26918,
                            coordinates: [[30, 10], [10, 30], [40, 40]]
                        }) {
                            type
                            crs
                            coordinates
                    }
                }");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Execute_CrsEqualToDefault_NotTransformArgument()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType<Query>()
            .AddSpatialTypes(x => x
                .DefaultSrid(4326)
                .AddCoordinateSystemFromString(4326, WKT4326)
                .AddCoordinateSystemFromString(26918, WKT26918))
            .TryAddTypeInterceptor<RoundTypeIntercetor>()
            .Create();

        var executor = schema.MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            @"
                {
                    test(
                        arg: {
                            type: LineString,
                            crs: 4326,
                            coordinates: [[30, 10], [10, 30], [40, 40]]
                        }) {
                            type
                            crs
                            coordinates
                    }
                }");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Execute_CrsEmpty_NotTransformArgument()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType<Query>()
            .AddSpatialTypes(x => x
                .DefaultSrid(4326)
                .AddCoordinateSystemFromString(4326, WKT4326)
                .AddCoordinateSystemFromString(26918, WKT26918))
            .TryAddTypeInterceptor<RoundTypeIntercetor>()
            .Create();

        var executor = schema.MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            @"
                {
                    test(
                        arg: {
                            type: LineString,
                            coordinates: [[30, 10], [10, 30], [40, 40]]
                        }) {
                            type
                            crs
                            coordinates
                    }
                }");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Execute_NoDefault_NotTransformation()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType<Query>()
            .AddSpatialTypes(x => x
                .AddCoordinateSystemFromString(4326, WKT4326)
                .AddCoordinateSystemFromString(26918, WKT26918))
            .TryAddTypeInterceptor<RoundTypeIntercetor>()
            .Create();

        var executor = schema.MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            @"
                {
                    test(
                        arg: {
                            type: LineString,
                            crs: 1234
                            coordinates: [[30, 10], [10, 30], [40, 40]]
                        }) {
                            type
                            crs
                            coordinates
                    }
                }");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Execute_CrsEmpty_TakeDefaultSrid()
    {
        // arrange
        var lineString = new LineString(
        [
            new Coordinate(30, 10),
                new Coordinate(10, 30),
                new CoordinateZ(10, 30, 12),
                new Coordinate(40, 40),
        ]);

        var schema = SchemaBuilder.New()
            .AddDocumentFromString(
                @"
                        type Query {
                            test: GeoJSONLineStringType
                        }
                    ")
            .AddSpatialTypes(x => x
                .DefaultSrid(4326)
                .AddCoordinateSystemFromString(4326, WKT4326)
                .AddCoordinateSystemFromString(26918, WKT26918))
            .AddResolver("Query", "test", _ => lineString)
            .TryAddTypeInterceptor<RoundTypeIntercetor>()
            .Create();

        var executor = schema.MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            @"{
                    test(crs: 26918) {
                        type
                        crs
                        coordinates
                    }
                }");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Execute_Transformer_TransformedArgument()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType<Query>()
            .AddSpatialTypes(x => x
                .DefaultSrid(4326)
                .AddCoordinateSystemFromString(4326, WKT4326)
                .AddCoordinateSystemFromString(26918, WKT26918))
            .TryAddTypeInterceptor<RoundTypeIntercetor>()
            .Create();

        var executor = schema.MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            @"
                {
                    test(
                        arg: {
                            type: LineString,
                            crs: 26918,
                            coordinates: [[30, 10], [10, 30], [40, 40]]
                        }) {
                            type
                            crs
                            coordinates
                    }
                }");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Execute_Transformer_TransformArgumentAndBack()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType<Query>()
            .AddSpatialTypes(x => x
                .DefaultSrid(4326)
                .AddCoordinateSystemFromString(4326, WKT4326)
                .AddCoordinateSystemFromString(26918, WKT26918))
            .TryAddTypeInterceptor<RoundTypeIntercetor>()
            .Create();

        var executor = schema.MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            @"
                {
                    test(
                        arg: {
                            type: LineString,
                            crs: 26918,
                            coordinates: [[30, 10], [10, 30], [40, 40]]
                        },
                        crs: 26918) {
                            type
                            crs
                            coordinates
                    }
                }");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Execute_SourceDifferentThanCrsRequest_Transform()
    {
        // arrange
        var lineString = NtsGeometryServices.Instance
            .CreateGeometryFactory(4326)
            .CreateLineString(
            [
                new Coordinate(30, 10),
                    new Coordinate(10, 30),
                    new Coordinate(40, 40),
            ]);

        var schema = SchemaBuilder.New()
            .AddDocumentFromString(
                @"
                        type Query {
                            test: GeoJSONLineStringType
                        }
                    ")
            .AddSpatialTypes(x => x
                .DefaultSrid(4326)
                .AddCoordinateSystemFromString(26918, WKT26918)
                .AddCoordinateSystemFromString(4326, WKT4326))
            .AddResolver("Query", "test", _ => lineString)
            .TryAddTypeInterceptor<RoundTypeIntercetor>()
            .Create();

        var executor = schema.MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            @"
                {
                    test(crs: 26918) {
                        type
                        crs
                        coordinates
                    }
                }");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Execute_SourceEqualToCrsRequest_NotTransform()
    {
        // arrange
        var lineString = NtsGeometryServices.Instance
            .CreateGeometryFactory(4326)
            .CreateLineString(
            [
                new Coordinate(30, 10),
                    new Coordinate(10, 30),
                    new Coordinate(40, 40),
            ]);

        var schema = SchemaBuilder.New()
            .AddDocumentFromString(
                @"
                        type Query {
                            test: GeoJSONLineStringType
                        }
                    ")
            .AddSpatialTypes(x => x
                .DefaultSrid(4326)
                .AddCoordinateSystemFromString(26918, WKT26918)
                .AddCoordinateSystemFromString(4326, WKT4326))
            .AddResolver("Query", "test", _ => lineString)
            .TryAddTypeInterceptor<RoundTypeIntercetor>()
            .Create();

        var executor = schema.MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            @"
                {
                    test(crs: 4326) {
                        type
                        crs
                        coordinates
                    }
                }");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Execute_Transformer_RegisterWithExtensions()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType<Query>()
            .AddSpatialTypes(x => x
                .DefaultSrid(4326)
                .AddWebMercator()
                .AddWGS84())
            .TryAddTypeInterceptor<RoundTypeIntercetor>()
            .Create();

        var executor = schema.MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            @"
                {
                    test(
                        arg: {
                            type: LineString,
                            crs: 3857,
                            coordinates: [[30, 10], [10, 30], [40, 40]]
                        }) {
                            type
                            crs
                            coordinates
                    }
                }");

        // assert
        result.MatchSnapshot();
    }

    public class Query
    {
        public LineString Test(LineString arg) => arg;
    }

    /// <summary>
    /// The transformation are different from system to system. We need to round the result
    /// to make it consistent!
    /// </summary>
    public class RoundTypeIntercetor : TypeInterceptor
    {
        public override void OnBeforeCompleteType(
            ITypeCompletionContext completionContext,
            DefinitionBase definition)
        {
            if (definition is ObjectTypeDefinition o)
            {
                foreach (var field in o.Fields)
                {
                    if (field.Name.EqualsOrdinal("test"))
                    {
                        field.MiddlewareDefinitions.Insert(0,
                            new(next => async context =>
                            {
                                await next(context);
                                if (context.Result is Geometry g)
                                {
                                    foreach (var coordinate in g.Coordinates)
                                    {
                                        if (coordinate is CoordinateZ z)
                                        {
                                            z.Z = Math.Round(z.Z, 2);
                                        }

                                        coordinate.Y = Math.Round(coordinate.Y, 2);
                                        coordinate.X = Math.Round(coordinate.X, 2);
                                    }
                                }
                            }
                        ));
                    }
                }
            }
        }
    }
}
