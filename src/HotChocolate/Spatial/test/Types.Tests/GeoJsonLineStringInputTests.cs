using System;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using NetTopologySuite.Geometries;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Spatial
{
    public class GeoJsonLineStringInputTests
    {
        private readonly ListValueNode _linestring = new ListValueNode(
            new ListValueNode(
                new IntValueNode(30),
                new IntValueNode(10)),
            new ListValueNode(
                new IntValueNode(10),
                new IntValueNode(30)),
            new ListValueNode(
                new IntValueNode(40),
                new IntValueNode(40)));

        private ISchema CreateSchema() => SchemaBuilder.New()
            .AddConvention<INamingConventions, MockNamingConvention>()
            .AddQueryType(d => d
                .Name("Query")
                .Field("test")
                .Argument("arg", a => a.Type<GeoJsonLineStringInputType>())
                .Resolver("ghi"))
            .Create();

        private InputObjectType CreateInputType()
        {
            ISchema schema = CreateSchema();
            return schema.GetType<InputObjectType>("GeoJSONLineStringInput");
        }

        [Fact]
        public void ParseLiteral_LineString_With_Valid_Coordinates()
        {
            // arrange
            InputObjectType type = CreateInputType();

            // act
            object? result = type.ParseLiteral(
                new ObjectValueNode(
                    new ObjectFieldNode("type", new EnumValueNode("LineString")),
                    new ObjectFieldNode("coordinates", _linestring)));

            // assert
            Assert.Equal(3, Assert.IsType<LineString>(result).NumPoints);
            Assert.Equal(30, Assert.IsType<LineString>(result).Coordinates[0].X);
            Assert.Equal(10, Assert.IsType<LineString>(result).Coordinates[0].Y);
            Assert.Equal(10, Assert.IsType<LineString>(result).Coordinates[1].X);
            Assert.Equal(30, Assert.IsType<LineString>(result).Coordinates[1].Y);
            Assert.Equal(40, Assert.IsType<LineString>(result).Coordinates[2].X);
            Assert.Equal(40, Assert.IsType<LineString>(result).Coordinates[2].Y);
        }

        [Fact]
        public void ParseLiteral_LineString_With_Valid_Coordinates_And_CRS()
        {
            // arrange
            InputObjectType type = CreateInputType();

            // act
            object? result = type.ParseLiteral(
                new ObjectValueNode(
                    new ObjectFieldNode("type", new EnumValueNode("LineString")),
                    new ObjectFieldNode("coordinates", _linestring),
                    new ObjectFieldNode("crs", 26912)));

            // assert
            Assert.Equal(3, Assert.IsType<LineString>(result).NumPoints);
            Assert.Equal(30, Assert.IsType<LineString>(result).Coordinates[0].X);
            Assert.Equal(10, Assert.IsType<LineString>(result).Coordinates[0].Y);
            Assert.Equal(10, Assert.IsType<LineString>(result).Coordinates[1].X);
            Assert.Equal(30, Assert.IsType<LineString>(result).Coordinates[1].Y);
            Assert.Equal(40, Assert.IsType<LineString>(result).Coordinates[2].X);
            Assert.Equal(40, Assert.IsType<LineString>(result).Coordinates[2].Y);
            Assert.Equal(26912, Assert.IsType<LineString>(result).SRID);
        }

        [Fact]
        public void ParseLiteral_LineString_Is_Null()
        {
            // arrange
            InputObjectType type = CreateInputType();

            // act
            object? result = type.ParseLiteral(NullValueNode.Default);

            // assert
            Assert.Null(result);
        }

        [Fact]
        public void ParseLiteral_LineString_Is_Not_ObjectType_Throws()
        {
            // arrange
            InputObjectType type = CreateInputType();

            // act
            // assert
            Assert.Throws<InvalidOperationException>(
                () => type.ParseLiteral(new ListValueNode()));
        }

        [Fact]
        public void ParseLiteral_LineString_With_Missing_Fields_Throws()
        {
            // arrange
            InputObjectType type = CreateInputType();

            // act
            // assert
            Assert.Throws<SerializationException>(
                () => type.ParseLiteral(
                    new ObjectValueNode(
                        new ObjectFieldNode("coordinates", _linestring),
                        new ObjectFieldNode("missingType", new StringValueNode("ignored")))));
        }

        [Fact]
        public void ParseLiteral_LineString_With_Empty_Coordinates_Throws()
        {
            // arrange
            InputObjectType type = CreateInputType();

            // act
            // assert
            Assert.Throws<SerializationException>(
                () => type.ParseLiteral(
                    new ObjectValueNode(
                        new ObjectFieldNode(
                            "type",
                            new EnumValueNode("LineString")),
                        new ObjectFieldNode("coordinates", new ListValueNode()))));
        }

        [Fact]
        public void ParseLiteral_LineString_With_Wrong_Geometry_Type_Throws()
        {
            // arrange
            InputObjectType type = CreateInputType();

            // act
            // assert
            Assert.Throws<SerializationException>(
                () => type.ParseLiteral(
                    new ObjectValueNode(
                        new ObjectFieldNode("type", new EnumValueNode("POLYGON")),
                        new ObjectFieldNode("coordinates", _linestring))));
        }

        [Fact]
        public async Task Execution_Tests()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(d => d
                    .Name("Query")
                    .Field("test")
                    .Argument("arg", a => a.Type<GeoJsonLineStringInputType>())
                    .Resolver(ctx => ctx.ArgumentValue<LineString>("arg").ToString()))
                .Create();

            IRequestExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                "{ test(arg: { type: LineString, coordinates: [[30, 10], [10, 30], [40, 40]]})}");

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void Schema_Tests() =>
            CreateSchema()
                .Print()
                .MatchSnapshot();

        [Fact]
        public void ParseLiteral_With_Input_Crs()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(d => d
                    .Name("Query")
                    .Field("test")
                    .Argument("arg", a => a.Type<GeoJsonLineStringInputType>())
                    .Resolver("ghi"))
                .Create();

            InputObjectType type = schema.GetType<InputObjectType>("GeoJSONLineStringInput");

            var node = new ObjectValueNode(
                new ObjectFieldNode("type", new EnumValueNode("LineString")),
                new ObjectFieldNode("coordinates", _linestring),
                new ObjectFieldNode("crs", 26912));

            // act
            object? result = type.ParseLiteral(node);

            // assert
            Assert.Equal(26912, Assert.IsType<LineString>(result).SRID);
        }

        [Fact]
        public async Task Execute_NoTransformer_NotTransformedInput()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<Query>()
                .AddSpatialTypes()
                .Create();

            IRequestExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
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
        public async Task Execute_Transformer_TransformedArgument()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<Query>()
                .AddSpatialTypes(x => x
                    .DefaultSrid(4326)
                    .AddCoordinateSystemFromString(4326,
                        "GEOGCS[\"WGS 84\",DATUM[\"WGS_1984\",SPHEROID[\"WGS 84\",6378137,298.257223563,AUTHORITY[\"EPSG\",\"7030\"]],AUTHORITY[\"EPSG\",\"6326\"]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.0174532925199433,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"4326\"]]")
                    .AddCoordinateSystemFromString(26918,
                        "PROJCS[\"NAD83 \\/ UTM zone 18N\",GEOGCS[\"NAD83\",DATUM[\"North_American_Datum_1983\",SPHEROID[\"GRS 1980\",6378137,298.257222101,AUTHORITY[\"EPSG\",\"7019\"]],TOWGS84[0,0,0,0,0,0,0],AUTHORITY[\"EPSG\",\"6269\"]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.0174532925199433,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"4269\"]],PROJECTION[\"Transverse_Mercator\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",-75],PARAMETER[\"scale_factor\",0.9996],PARAMETER[\"false_easting\",500000],PARAMETER[\"false_northing\",0],UNIT[\"metre\",1,AUTHORITY[\"EPSG\",\"9001\"]],AXIS[\"Easting\",EAST],AXIS[\"Northing\",NORTH],AUTHORITY[\"EPSG\",\"26918\"]]"
                    ))
                .Create();

            IRequestExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
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
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<Query>()
                .AddSpatialTypes(x => x
                    .DefaultSrid(4326)
                    .AddCoordinateSystemFromString(4326,
                        "GEOGCS[\"WGS 84\",DATUM[\"WGS_1984\",SPHEROID[\"WGS 84\",6378137,298.257223563,AUTHORITY[\"EPSG\",\"7030\"]],AUTHORITY[\"EPSG\",\"6326\"]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.0174532925199433,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"4326\"]]")
                    .AddCoordinateSystemFromString(26918,
                        "PROJCS[\"NAD83 \\/ UTM zone 18N\",GEOGCS[\"NAD83\",DATUM[\"North_American_Datum_1983\",SPHEROID[\"GRS 1980\",6378137,298.257222101,AUTHORITY[\"EPSG\",\"7019\"]],TOWGS84[0,0,0,0,0,0,0],AUTHORITY[\"EPSG\",\"6269\"]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.0174532925199433,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"4269\"]],PROJECTION[\"Transverse_Mercator\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",-75],PARAMETER[\"scale_factor\",0.9996],PARAMETER[\"false_easting\",500000],PARAMETER[\"false_northing\",0],UNIT[\"metre\",1,AUTHORITY[\"EPSG\",\"9001\"]],AXIS[\"Easting\",EAST],AXIS[\"Northing\",NORTH],AUTHORITY[\"EPSG\",\"26918\"]]"
                    ))
                .Create();

            IRequestExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
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

        public class Query
        {
            public LineString Test(LineString arg) => arg;
        }
    }
}
