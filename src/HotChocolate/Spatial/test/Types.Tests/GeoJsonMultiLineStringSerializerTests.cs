using System;
using System.Collections.Generic;
using HotChocolate.Language;
using NetTopologySuite.Geometries;
using Snapshooter.Xunit;
using Xunit;
using static HotChocolate.Types.Spatial.WellKnownTypeNames;

namespace HotChocolate.Types.Spatial
{
    public class GeoJsonMultiLineStringSerializerTests
    {
        private readonly IValueNode _coordinatesSyntaxNode = new ListValueNode(
            new ListValueNode(
                new ListValueNode(
                    new IntValueNode(10),
                    new IntValueNode(10)),
                new ListValueNode(
                    new IntValueNode(20),
                    new IntValueNode(20)),
                new ListValueNode(
                    new IntValueNode(10),
                    new IntValueNode(40))),
            new ListValueNode(
                new ListValueNode(
                    new IntValueNode(40),
                    new IntValueNode(40)),
                new ListValueNode(
                    new IntValueNode(30),
                    new IntValueNode(30)),
                new ListValueNode(
                    new IntValueNode(40),
                    new IntValueNode(20)),
                new ListValueNode(
                    new IntValueNode(30),
                    new IntValueNode(10))
            ));

        private Geometry _geometry = new MultiLineString(
            new[]
            {
                new LineString(
                    new[]
                    {
                        new Coordinate(10, 10), new Coordinate(20, 20), new Coordinate(10, 40)
                    }),
                new LineString(
                    new[]
                    {
                        new Coordinate(40, 40),
                        new Coordinate(30, 30),
                        new Coordinate(40, 20),
                        new Coordinate(30, 10)
                    })
            });

        private readonly string _geometryType = "MultiLineString";

        private readonly object _geometryParsed = new[]
        {
            new[] { new[] { 10.0, 10.0 }, new[] { 20.0, 20.0 }, new[] { 10.0, 40.0 } },
            new[]
            {
                new[] { 40.0, 40.0 },
                new[] { 30.0, 30.0 },
                new[] { 40.0, 20.0 },
                new[] { 30.0, 10.0 }
            }
        };

        [Theory]
        [InlineData(MultiLineStringInputName)]
        [InlineData(GeometryTypeName)]
        public void Serialize_Should_Pass_When_SerializeNullValue(string typeName)
        {
            // arrange
            INamedInputType type = CreateInputType(typeName);

            // act
            // assert
            Assert.Null(type.Serialize(null));
        }

        [Theory]
        [InlineData(MultiLineStringInputName)]
        [InlineData(GeometryTypeName)]
        public void Serialize_Should_Pass_When_Dictionary(string typeName)
        {
            // arrange
            INamedInputType type = CreateInputType(typeName);
            var dictionary = new Dictionary<string, object>();

            // act
            object? result = type.Serialize(dictionary);

            // assert
            Assert.Equal(dictionary, result);
        }

        [Theory]
        [InlineData(MultiLineStringInputName)]
        [InlineData(GeometryTypeName)]
        public void Serialize_Should_Pass_When_SerializeGeometry(string typeName)
        {
            // arrange
            INamedInputType type = CreateInputType(typeName);

            // act
            object? result = type.Serialize(_geometry);

            // assert
            result.MatchSnapshot();
        }

        [Theory]
        [InlineData(MultiLineStringInputName)]
        [InlineData(GeometryTypeName)]
        public void Serialize_Should_Throw_When_InvalidObjectShouldThrow(string typeName)
        {
            // arrange
            INamedInputType type = CreateInputType(typeName);

            // act
            // assert
            Assert.Throws<SerializationException>(() => type.Serialize(""));
        }

        [Theory]
        [InlineData(MultiLineStringInputName)]
        [InlineData(GeometryTypeName)]
        public void IsInstanceOfType_Should_Throw_When_Null(string typeName)
        {
            // arrange
            INamedInputType type = CreateInputType(typeName);

            // act
            // assert
            Assert.Throws<ArgumentNullException>(() => type.IsInstanceOfType(null!));
        }

        [Theory]
        [InlineData(MultiLineStringInputName)]
        public void IsInstanceOfType_Should_Pass_When_ObjectValueNode(string typeName)
        {
            // arrange
            INamedInputType type = CreateInputType(typeName);

            // act
            // assert
            Assert.True(type.IsInstanceOfType(new ObjectValueNode()));
        }

        [Theory]
        [InlineData(MultiLineStringInputName)]
        [InlineData(GeometryTypeName)]
        public void IsInstanceOfType_Should_Pass_When_NullValueNode(string typeName)
        {
            // arrange
            INamedInputType type = CreateInputType(typeName);

            // act
            // assert
            Assert.True(type.IsInstanceOfType(NullValueNode.Default));
        }

        [Theory]
        [InlineData(MultiLineStringInputName)]
        [InlineData(GeometryTypeName)]
        public void IsInstanceOfType_Should_Fail_When_DifferentGeoJsonObject(string typeName)
        {
            // arrange
            INamedInputType type = CreateInputType(typeName);

            // act
            // assert
            Assert.False(
                type.IsInstanceOfType(
                    GeometryFactory.Default.CreateGeometryCollection(new[] { new Point(1, 2) })));
        }

        [Theory]
        [InlineData(MultiLineStringInputName)]
        [InlineData(GeometryTypeName)]
        public void IsInstanceOfType_Should_Pass_When_GeometryOfType(string typeName)
        {
            // arrange
            INamedInputType type = CreateInputType(typeName);

            // act
            // assert
            Assert.True(type.IsInstanceOfType(_geometry));
        }

        [Theory]
        [InlineData(MultiLineStringInputName)]
        [InlineData(GeometryTypeName)]
        public void IsInstanceOfType_Should_Fail_When_NoGeometry(string typeName)
        {
            // arrange
            INamedInputType type = CreateInputType(typeName);

            // act
            // assert
            Assert.False(type.IsInstanceOfType("foo"));
        }

        [Theory]
        [InlineData(MultiLineStringInputName)]
        [InlineData(GeometryTypeName)]
        public void ParseLiteral_Should_Pass_When_NullValueNode(string typeName)
        {
            // arrange
            INamedInputType type = CreateInputType(typeName);

            // act
            // assert
            Assert.Null(type.ParseLiteral(NullValueNode.Default));
        }

        [Theory]
        [InlineData(MultiLineStringInputName)]
        [InlineData(GeometryTypeName)]
        public void ParseLiteral_Should_Throw_When_NotObjectValueNode(string typeName)
        {
            // arrange
            INamedInputType type = CreateInputType(typeName);

            // act
            // assert
            Assert.Throws<InvalidOperationException>(() => type.ParseLiteral(new ListValueNode()));
        }

        [Theory]
        [InlineData(MultiLineStringInputName)]
        [InlineData(GeometryTypeName)]
        public void ParseLiteral_Should_Pass_When_CorrectGeometry(string typeName)
        {
            // arrange
            INamedInputType type = CreateInputType(typeName);
            var typeField = new ObjectFieldNode(WellKnownFields.TypeFieldName, _geometryType);
            var coordField = new ObjectFieldNode(
                WellKnownFields.CoordinatesFieldName,
                _coordinatesSyntaxNode);
            var crsField = new ObjectFieldNode(WellKnownFields.CrsFieldName, 26912);
            var valueNode = new ObjectValueNode(typeField, coordField, crsField);

            // act
            object? parsedResult = type.ParseLiteral(valueNode);

            // assert
            AssertGeometry(parsedResult, 26912);
        }

        [Theory]
        [InlineData(MultiLineStringInputName)]
        [InlineData(GeometryTypeName)]
        public void ParseLiteral_Should_Throw_When_NoGeometryType(string typeName)
        {
            // arrange
            INamedInputType type = CreateInputType(typeName);
            var coordField = new ObjectFieldNode(
                WellKnownFields.CoordinatesFieldName,
                _coordinatesSyntaxNode);
            var crsField = new ObjectFieldNode(WellKnownFields.CrsFieldName, 0);
            var valueNode = new ObjectValueNode(coordField, crsField);

            // act
            Assert.Throws<SerializationException>(() => type.ParseLiteral(valueNode));
        }

        [Theory]
        [InlineData(MultiLineStringInputName)]
        [InlineData(GeometryTypeName)]
        public void ParseLiteral_Should_Throw_When_NoCoordinates(string typeName)
        {
            // arrange
            INamedInputType type = CreateInputType(typeName);
            var typeField = new ObjectFieldNode(WellKnownFields.TypeFieldName, _geometryType);
            var crsField = new ObjectFieldNode(WellKnownFields.CrsFieldName, 0);
            var valueNode = new ObjectValueNode(typeField, crsField);

            // act
            Assert.Throws<SerializationException>(() => type.ParseLiteral(valueNode));
        }

        [Theory]
        [InlineData(MultiLineStringInputName)]
        [InlineData(GeometryTypeName)]
        public void ParseLiteral_Should_Pass_When_NoCrs(string typeName)
        {
            // arrange
            INamedInputType type = CreateInputType(typeName);
            var typeField = new ObjectFieldNode(WellKnownFields.TypeFieldName, _geometryType);
            var coordField = new ObjectFieldNode(
                WellKnownFields.CoordinatesFieldName,
                _coordinatesSyntaxNode);
            var valueNode = new ObjectValueNode(typeField, coordField);

            // act
            object? parsedResult = type.ParseLiteral(valueNode);

            // assert
            AssertGeometry(parsedResult);
        }

        [Theory]
        [InlineData(MultiLineStringInputName)]
        [InlineData(GeometryTypeName)]
        public void ParseResult_Should_Pass_When_NullValue(string typeName)
        {
            // arrange
            INamedInputType type = CreateInputType(typeName);

            // act
            // assert
            Assert.Equal(NullValueNode.Default, type.ParseValue(null));
        }

        [Theory]
        [InlineData(MultiLineStringInputName)]
        [InlineData(GeometryTypeName)]
        public void ParseResult_Should_Pass_When_Serialized(string typeName)
        {
            // arrange
            INamedInputType type = CreateInputType(typeName);

            object? serialized = type.Serialize(_geometry);

            // act
            IValueNode literal = type.ParseResult(serialized);

            // assert
            literal.ToString().MatchSnapshot();
        }

        [Theory]
        [InlineData(MultiLineStringInputName)]
        [InlineData(GeometryTypeName)]
        public void ParseResult_Should_Pass_When_Value(string typeName)
        {
            // arrange
            INamedInputType type = CreateInputType(typeName);

            // act
            IValueNode literal = type.ParseResult(_geometry);

            // assert
            literal.ToString().MatchSnapshot();
        }

        [Theory]
        [InlineData(MultiLineStringInputName)]
        [InlineData(GeometryTypeName)]
        public void ParseResult_Should_Throw_When_InvalidType(string typeName)
        {
            // arrange
            INamedInputType type = CreateInputType(typeName);

            // act
            // assert
            Assert.Throws<SerializationException>(() => type.ParseResult(""));
        }

        [Theory]
        [InlineData(MultiLineStringInputName)]
        [InlineData(GeometryTypeName)]
        public void ParseValue_Should_Pass_When_NullValue(string typeName)
        {
            // arrange
            INamedInputType type = CreateInputType(typeName);

            // act
            // assert
            Assert.Equal(NullValueNode.Default, type.ParseValue(null));
        }

        [Theory]
        [InlineData(MultiLineStringInputName)]
        [InlineData(GeometryTypeName)]
        public void ParseValue_Should_Pass_When_Serialized(string typeName)
        {
            // arrange
            INamedInputType type = CreateInputType(typeName);

            object? serialized = type.Serialize(_geometry);

            // act
            IValueNode literal = type.ParseValue(serialized);

            // assert
            literal.ToString().MatchSnapshot();
        }

        [Theory]
        [InlineData(MultiLineStringInputName)]
        [InlineData(GeometryTypeName)]
        public void ParseValue_Should_Pass_When_Value(string typeName)
        {
            // arrange
            INamedInputType type = CreateInputType(typeName);

            // act
            IValueNode literal = type.ParseValue(_geometry);

            // assert
            literal.ToString().MatchSnapshot();
        }

        [Theory]
        [InlineData(MultiLineStringInputName)]
        [InlineData(GeometryTypeName)]
        public void ParseValue_Should_Throw_When_InvalidType(string typeName)
        {
            // arrange
            INamedInputType type = CreateInputType(typeName);

            // act
            // assert
            Assert.Throws<SerializationException>(() => type.ParseValue(""));
        }

        [Theory]
        [InlineData(MultiLineStringInputName)]
        [InlineData(GeometryTypeName)]
        public void Deserialize_Should_Pass_When_SerializeNullValue(string typeName)
        {
            INamedInputType type = CreateInputType(typeName);

            Assert.Null(type.Deserialize(null));
        }

        [Theory]
        [InlineData(MultiLineStringInputName)]
        [InlineData(GeometryTypeName)]
        public void Deserialize_Should_Pass_When_PassedSerializedResult(string typeName)
        {
            // arrange
            INamedInputType type = CreateInputType(typeName);

            object? serialized = type.Serialize(_geometry);

            // act
            object? result = type.Deserialize(serialized);

            // assert
            Assert.True(Assert.IsAssignableFrom<Geometry>(result).Equals(_geometry));
        }

        [Theory]
        [InlineData(MultiLineStringInputName)]
        [InlineData(GeometryTypeName)]
        public void Deserialize_Should_Pass_When_SerializeGeometry(string typeName)
        {
            // arrange
            INamedInputType type = CreateInputType(typeName);

            // act
            object? result = type.Deserialize(_geometry);

            // assert
            Assert.Equal(result, _geometry);
        }

        [Theory]
        [InlineData(MultiLineStringInputName)]
        [InlineData(GeometryTypeName)]
        public void Deserialize_Should_Throw_When_InvalidType(string typeName)
        {
            // arrange
            INamedInputType type = CreateInputType(typeName);

            // act
            // assert
            Assert.Throws<SerializationException>(() => type.Deserialize(""));
        }

        [Theory]
        [InlineData(MultiLineStringInputName)]
        [InlineData(GeometryTypeName)]
        public void Deserialize_Should_Pass_When_AllFieldsInDictionary(string typeName)
        {
            // arrange
            INamedInputType type = CreateInputType(typeName);

            var serialized = new Dictionary<string, object>
            {
                { WellKnownFields.TypeFieldName, _geometryType },
                { WellKnownFields.CoordinatesFieldName, _geometryParsed },
                { WellKnownFields.CrsFieldName, 26912 }
            };

            // act
            object? result = type.Deserialize(serialized);

            // assert
            AssertGeometry(result, 26912);
        }

        [Theory]
        [InlineData(MultiLineStringInputName)]
        [InlineData(GeometryTypeName)]
        public void Deserialize_Should_Pass_When_CrsIsMissing(string typeName)
        {
            // arrange
            INamedInputType type = CreateInputType(typeName);

            var serialized = new Dictionary<string, object>
            {
                { WellKnownFields.TypeFieldName, _geometryType },
                { WellKnownFields.CoordinatesFieldName, _geometryParsed }
            };

            // act
            object? result = type.Deserialize(serialized);

            // assert
            AssertGeometry(result);
        }

        [Theory]
        [InlineData(MultiLineStringInputName)]
        [InlineData(GeometryTypeName)]
        public void Deserialize_Should_Fail_WhentypeNameIsMissing(string typeName)
        {
            // arrange
            INamedInputType type = CreateInputType(typeName);

            var serialized = new Dictionary<string, object>
            {
                { WellKnownFields.CoordinatesFieldName, _geometryParsed },
                { WellKnownFields.CrsFieldName, new IntValueNode(0) }
            };

            // act
            // assert
            Assert.Throws<SerializationException>(() => type.Deserialize(serialized));
        }

        [Theory]
        [InlineData(MultiLineStringInputName)]
        [InlineData(GeometryTypeName)]
        public void Deserialize_Should_When_CoordinatesAreMissing(string typeName)
        {
            // arrange
            INamedInputType type = CreateInputType(typeName);

            var serialized = new Dictionary<string, object>
            {
                { WellKnownFields.TypeFieldName, _geometryType },
                { WellKnownFields.CrsFieldName, new IntValueNode(0) }
            };

            // act
            // assert
            Assert.Throws<SerializationException>(() => type.Deserialize(serialized));
        }

        [Theory]
        [InlineData(MultiLineStringInputName)]
        [InlineData(GeometryTypeName)]
        public void MultiLine_IsCoordinateValid_Should_Fail_When_Point(string typeName)
        {
            INamedInputType type = CreateInputType(typeName);

            var coords = new ListValueNode(
                new IntValueNode(30),
                new IntValueNode(10));
            var typeField = new ObjectFieldNode(WellKnownFields.TypeFieldName, _geometryType);
            var coordField = new ObjectFieldNode(WellKnownFields.CoordinatesFieldName, coords);
            var valueNode = new ObjectValueNode(typeField, coordField);

            // act
            // assert
            Assert.Throws<SerializationException>(() => type.ParseLiteral(valueNode));
        }

        private ISchema CreateSchema() => SchemaBuilder.New()
            .AddSpatialTypes()
            .AddQueryType(
                d => d
                    .Name("Query")
                    .Field("test")
                    .Argument("arg", a => a.Type<StringType>())
                    .Resolver("ghi"))
            .Create();

        private static void AssertGeometry(object? obj, int? crs = null)
        {
            Assert.Equal(2, Assert.IsType<MultiLineString>(obj).NumGeometries);
            Assert.Equal(3, Assert.IsType<MultiLineString>(obj).Geometries[0].NumPoints);
            Assert.Equal(4, Assert.IsType<MultiLineString>(obj).Geometries[1].NumPoints);

            Assert.Equal(10, Assert.IsType<MultiLineString>(obj).Coordinates[0].X);
            Assert.Equal(10, Assert.IsType<MultiLineString>(obj).Coordinates[0].Y);
            Assert.Equal(20, Assert.IsType<MultiLineString>(obj).Coordinates[1].X);
            Assert.Equal(20, Assert.IsType<MultiLineString>(obj).Coordinates[1].Y);
            Assert.Equal(10, Assert.IsType<MultiLineString>(obj).Coordinates[2].X);
            Assert.Equal(40, Assert.IsType<MultiLineString>(obj).Coordinates[2].Y);

            if (crs is not null)
            {
                Assert.Equal(crs, Assert.IsType<MultiLineString>(obj).SRID);
            }
        }

        private INamedInputType CreateInputType(string typeName)
        {
            ISchema schema = CreateSchema();

            return schema.GetType<INamedInputType>(typeName);
        }
    }
}
