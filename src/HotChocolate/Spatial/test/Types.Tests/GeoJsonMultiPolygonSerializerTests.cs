using System;
using System.Collections.Generic;
using HotChocolate.Language;
using NetTopologySuite.Geometries;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Spatial.Tests
{
    public class GeoJsonMultiPolygonSerializerTests
    {
        private readonly IValueNode _coordinatesSyntaxNode = new ListValueNode(
            new ListValueNode(
                new ListValueNode(
                    new IntValueNode(30),
                    new IntValueNode(20)),
                new ListValueNode(
                    new IntValueNode(45),
                    new IntValueNode(40)),
                new ListValueNode(
                    new IntValueNode(10),
                    new IntValueNode(40)),
                new ListValueNode(
                    new IntValueNode(30),
                    new IntValueNode(20))),
            new ListValueNode(
                new ListValueNode(
                    new IntValueNode(15),
                    new IntValueNode(5)),
                new ListValueNode(
                    new IntValueNode(40),
                    new IntValueNode(10)),
                new ListValueNode(
                    new IntValueNode(10),
                    new IntValueNode(20)),
                new ListValueNode(
                    new IntValueNode(5),
                    new IntValueNode(15)),
                new ListValueNode(
                    new IntValueNode(15),
                    new IntValueNode(5))));

        private Geometry _geometry = new MultiPolygon(
            new[]
            {
                new Polygon(
                    new LinearRing(
                        new[]
                        {
                            new Coordinate(30, 20),
                            new Coordinate(45, 40),
                            new Coordinate(10, 40),
                            new Coordinate(30, 20)
                        })),
                new Polygon(
                    new LinearRing(
                        new[]
                        {
                            new Coordinate(15, 5),
                            new Coordinate(40, 10),
                            new Coordinate(10, 20),
                            new Coordinate(5, 15),
                            new Coordinate(15, 5)
                        }))
            });

        private readonly string _geometryType = "MultiPolygon";

        private readonly object _geometryParsed;

        private readonly InputObjectType _type;

        public GeoJsonMultiPolygonSerializerTests()
        {
            ISchema schema = CreateSchema();

            _type = schema.GetType<InputObjectType>("GeoJSONMultiPolygonInput");
            _geometryParsed = new[]
            {
                new[]
                {
                    new[] { 30.0, 20.0 },
                    new[] { 45.0, 40.0 },
                    new[] { 10.0, 40.0 },
                    new[] { 30.0, 20.0 }
                },
                new[]
                {
                    new[] { 15.0, 5.0 },
                    new[] { 40.0, 10.0 },
                    new[] { 10.0, 20.0 },
                    new[] { 5.0, 15.0 },
                    new[] { 15.0, 5.0 }
                }
            };
        }

        [Fact]
        public void Serialize_Should_Pass_When_SerializeNullValue()
        {
            Assert.Null(_type.Serialize(null));
        }

        [Fact]
        public void Serialize_Should_Pass_When_Dictionary()
        {
            // arrange
            var dictionary = new Dictionary<string, object>();

            // act
            object? result = _type.Serialize(dictionary);

            // assert
            Assert.Equal(dictionary, result);
        }

        [Fact]
        public void Serialize_Should_Pass_When_SerializeGeometry()
        {
            // arrange
            // act
            object? result = _type.Serialize(_geometry);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void Serialize_Should_Throw_When_InvalidObjectShouldThrow()
        {
            // arrange
            // act
            // assert
            Assert.Throws<SerializationException>(() => _type.Serialize(""));
        }

        [Fact]
        public void IsInstanceOfType_Should_Throw_When_Null()
        {
            // arrange
            // act
            // assert
            Assert.Throws<ArgumentNullException>(() => _type.IsInstanceOfType(null!));
        }

        [Fact]
        public void IsInstanceOfType_Should_Pass_When_ObjectValueNode()
        {
            // arrange
            // act
            // assert
            Assert.True(_type.IsInstanceOfType(new ObjectValueNode()));
        }

        [Fact]
        public void IsInstanceOfType_Should_Pass_When_NullValueNode()
        {
            // arrange
            // act
            // assert
            Assert.True(_type.IsInstanceOfType(NullValueNode.Default));
        }

        [Fact]
        public void IsInstanceOfType_Should_Fail_When_DifferentGeoJsonObject()
        {
            // arrange
            // act
            // assert
            Assert.False(
                _type.IsInstanceOfType(
                    GeometryFactory.Default.CreateGeometryCollection(new[] { new Point(1, 2) })));
        }

        [Fact]
        public void IsInstanceOfType_Should_Pass_When_GeometryOfType()
        {
            // arrange
            // act
            // assert
            Assert.True(_type.IsInstanceOfType(_geometry));
        }

        [Fact]
        public void IsInstanceOfType_Should_Fail_When_NoGeometry()
        {
            // arrange
            // act
            // assert
            Assert.False(_type.IsInstanceOfType("foo"));
        }

        [Fact]
        public void ParseLiteral_Should_Pass_When_NullValueNode()
        {
            // arrange
            // act
            // assert
            Assert.Null(_type.ParseLiteral(NullValueNode.Default));
        }

        [Fact]
        public void ParseLiteral_Should_Throw_When_NotObjectValueNode()
        {
            // arrange
            // act
            // assert
            Assert.Throws<InvalidOperationException>(() => _type.ParseLiteral(new ListValueNode()));
        }

        [Fact]
        public void ParseLiteral_Should_Pass_When_CorrectGeometry()
        {
            // arrange
            var typeField = new ObjectFieldNode(WellKnownFields.TypeFieldName, _geometryType);
            var coordField = new ObjectFieldNode(WellKnownFields.CoordinatesFieldName, _coordinatesSyntaxNode);
            var crsField = new ObjectFieldNode(WellKnownFields.CrsFieldName, 26912);
            var valueNode = new ObjectValueNode(typeField, coordField, crsField);

            // act
            object? parsedResult = _type.ParseLiteral(valueNode);

            // assert
            AssertGeometry(parsedResult, 26912);
        }

        [Fact]
        public void ParseLiteral_Should_Throw_When_NoGeometryType()
        {
            // arrange
            var coordField = new ObjectFieldNode(WellKnownFields.CoordinatesFieldName, _coordinatesSyntaxNode);
            var crsField = new ObjectFieldNode(WellKnownFields.CrsFieldName, 0);
            var valueNode = new ObjectValueNode(coordField, crsField);

            // act
            Assert.Throws<SerializationException>(() => _type.ParseLiteral(valueNode));
        }

        [Fact]
        public void ParseLiteral_Should_Throw_When_NoCoordinates()
        {
            // arrange
            var typeField = new ObjectFieldNode(WellKnownFields.TypeFieldName, _geometryType);
            var crsField = new ObjectFieldNode(WellKnownFields.CrsFieldName, 0);
            var valueNode = new ObjectValueNode(typeField, crsField);

            // act
            Assert.Throws<SerializationException>(() => _type.ParseLiteral(valueNode));
        }

        [Fact]
        public void ParseLiteral_Should_Pass_When_NoCrs()
        {
            // arrange
            var typeField = new ObjectFieldNode(WellKnownFields.TypeFieldName, _geometryType);
            var coordField = new ObjectFieldNode(WellKnownFields.CoordinatesFieldName, _coordinatesSyntaxNode);
            var valueNode = new ObjectValueNode(typeField, coordField);

            // act
            object? parsedResult = _type.ParseLiteral(valueNode);

            // assert
            AssertGeometry(parsedResult);
        }

        [Fact]
        public void ParseResult_Should_Pass_When_NullValue()
        {
            // arrange
            // act
            // assert
            Assert.Equal(NullValueNode.Default, _type.ParseValue(null));
        }

        [Fact]
        public void ParseResult_Should_Pass_When_Serialized()
        {
            // arrange
            object? serialized = _type.Serialize(_geometry);

            // act
            IValueNode literal = _type.ParseResult(serialized);

            // assert
            literal.ToString().MatchSnapshot();
        }

        [Fact]
        public void ParseResult_Should_Pass_When_Value()
        {
            // arrange
            // act
            IValueNode literal = _type.ParseResult(_geometry);

            // assert
            literal.ToString().MatchSnapshot();
        }

        [Fact]
        public void ParseResult_Should_Throw_When_InvalidType()
        {
            // arrange
            // act
            // assert
            Assert.Throws<SerializationException>(() => _type.ParseResult(""));
        }

        [Fact]
        public void ParseValue_Should_Pass_When_NullValue()
        {
            // arrange
            // act
            // assert
            Assert.Equal(NullValueNode.Default, _type.ParseValue(null));
        }

        [Fact]
        public void ParseValue_Should_Pass_When_Serialized()
        {
            // arrange
            object? serialized = _type.Serialize(_geometry);

            // act
            IValueNode literal = _type.ParseValue(serialized);

            // assert
            literal.ToString().MatchSnapshot();
        }

        [Fact]
        public void ParseValue_Should_Pass_When_Value()
        {
            // arrange
            // act
            IValueNode literal = _type.ParseValue(_geometry);

            // assert
            literal.ToString().MatchSnapshot();
        }

        [Fact]
        public void ParseValue_Should_Throw_When_InvalidType()
        {
            // arrange
            // act
            // assert
            Assert.Throws<SerializationException>(() => _type.ParseValue(""));
        }

        [Fact]
        public void Deserialize_Should_Pass_When_SerializeNullValue()
        {
            Assert.Null(_type.Deserialize(null));
        }

        [Fact]
        public void Deserialize_Should_Pass_When_PassedSerializedResult()
        {
            // arrange
            object? serialized = _type.Serialize(_geometry);

            // act
            object? result = _type.Deserialize(serialized);

            // assert
            Assert.True(Assert.IsAssignableFrom<Geometry>(result).Equals(_geometry));
        }

        [Fact]
        public void Deserialize_Should_Pass_When_SerializeGeometry()
        {
            // arrange
            // act
            object? result = _type.Deserialize(_geometry);

            // assert
            Assert.Equal(result, _geometry);
        }

        [Fact]
        public void Deserialize_Should_Throw_When_InvalidType()
        {
            // arrange
            // act
            // assert
            Assert.Throws<SerializationException>(() => _type.Deserialize(""));
        }

        [Fact]
        public void Deserialize_Should_Pass_When_AllFieldsInDictionary()
        {
            // arrange
            var serialized = new Dictionary<string, object>
            {
                { WellKnownFields.TypeFieldName, _geometryType },
                { WellKnownFields.CoordinatesFieldName, _geometryParsed },
                { WellKnownFields.CrsFieldName, 26912 }
            };

            // act
            object? result = _type.Deserialize(serialized);

            // assert
            AssertGeometry(result, 26912);
        }

        [Fact]
        public void Deserialize_Should_Pass_When_CrsIsMissing()
        {
            // arrange
            var serialized = new Dictionary<string, object>
            {
                { WellKnownFields.TypeFieldName, _geometryType }, { WellKnownFields.CoordinatesFieldName, _geometryParsed }
            };

            // act
            object? result = _type.Deserialize(serialized);

            // assert
            AssertGeometry(result);
        }

        [Fact]
        public void Deserialize_Should_Fail_When_TypeNameIsMissing()
        {
            // arrange
            var serialized = new Dictionary<string, object>
            {
                { WellKnownFields.CoordinatesFieldName, _geometryParsed }, { WellKnownFields.CrsFieldName, new IntValueNode(0) }
            };

            // act
            // assert
            Assert.Throws<SerializationException>(() => _type.Deserialize(serialized));
        }

        [Fact]
        public void Deserialize_Should_When_CoordinatesAreMissing()
        {
            // arrange
            var serialized = new Dictionary<string, object>
            {
                { WellKnownFields.TypeFieldName, _geometryType }, { WellKnownFields.CrsFieldName, new IntValueNode(0) }
            };

            // act
            // assert
            Assert.Throws<SerializationException>(() => _type.Deserialize(serialized));
        }

        [Fact]
        public void MultiPolygon_IsCoordinateValid_Should_Fail_When_Point()
        {
            var coords = new ListValueNode(
                new IntValueNode(30),
                new IntValueNode(10));
            var typeField = new ObjectFieldNode(WellKnownFields.TypeFieldName, _geometryType);
            var coordField = new ObjectFieldNode(WellKnownFields.CoordinatesFieldName, coords);
            var valueNode = new ObjectValueNode(typeField, coordField);

            // act
            // assert
            Assert.Throws<SerializationException>(() => _type.ParseLiteral(valueNode));
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
            Assert.Equal(2, Assert.IsType<MultiPolygon>(obj).NumGeometries);
            Assert.Equal(4, Assert.IsType<MultiPolygon>(obj).Geometries[0].NumPoints);
            Assert.Equal(5, Assert.IsType<MultiPolygon>(obj).Geometries[1].NumPoints);

            Assert.Equal(30, Assert.IsType<MultiPolygon>(obj).Coordinates[0].X);
            Assert.Equal(20, Assert.IsType<MultiPolygon>(obj).Coordinates[0].Y);
            Assert.Equal(45, Assert.IsType<MultiPolygon>(obj).Coordinates[1].X);
            Assert.Equal(40, Assert.IsType<MultiPolygon>(obj).Coordinates[1].Y);
            Assert.Equal(10, Assert.IsType<MultiPolygon>(obj).Coordinates[2].X);
            Assert.Equal(40, Assert.IsType<MultiPolygon>(obj).Coordinates[2].Y);
            Assert.Equal(30, Assert.IsType<MultiPolygon>(obj).Coordinates[3].X);
            Assert.Equal(20, Assert.IsType<MultiPolygon>(obj).Coordinates[3].Y);

            if (crs is not null)
            {
                Assert.Equal(crs, Assert.IsType<MultiPolygon>(obj).SRID);
            }
        }
    }
}
