using HotChocolate.Language;
using NetTopologySuite.Geometries;
using Xunit;

namespace HotChocolate.Types.Spatial.Tests
{
    public class ParseLiteralHelperTests
    {
        private TestGeoJsonInputType mock = new TestGeoJsonInputType();

        [Fact]
        public void Helper_Finds_Fields_In_ObjectValueNode()
        {
            var indices = mock.GetFieldIndices(
                new ObjectValueNode(
                    new ObjectFieldNode("type", 1),
                    new ObjectFieldNode("coordinates", 2)));

            Assert.Equal(0, indices.typeIndex);
            Assert.Equal(1, indices.coordinateIndex);
        }

        [Fact]
        public void Helper_Finds_Partial_Fields_In_ObjectValueNode()
        {
            var indices = mock.GetFieldIndices(
                new ObjectValueNode(
                    new ObjectFieldNode("something", 1),
                    new ObjectFieldNode("other", 1),
                    new ObjectFieldNode("than", 1),
                    new ObjectFieldNode("it", 1),
                    new ObjectFieldNode("coordinates", 2)));

            Assert.Equal(-1, indices.typeIndex);
            Assert.Equal(4, indices.coordinateIndex);
        }

        [Fact]
        public void Helper_Finds_No_Fields_In_ObjectValueNode()
        {
            var indices = mock.GetFieldIndices(
                new ObjectValueNode(
                    new ObjectFieldNode("something", 1),
                    new ObjectFieldNode("other", 1),
                    new ObjectFieldNode("than", 1)));

            Assert.Equal(-1, indices.typeIndex);
            Assert.Equal(-1, indices.coordinateIndex);
        }

        [Fact]
        public void Helper_Finds_Partial_Ignores_Case()
        {
            var indices = mock.GetFieldIndices(
                new ObjectValueNode(
                    new ObjectFieldNode("TyPe", 1),
                    new ObjectFieldNode("COORDINATES", 2)));

            Assert.Equal(0, indices.typeIndex);
            Assert.Equal(1, indices.coordinateIndex);
        }

        [Fact]
        public void Helper_Finds_Partial_Exits_Early()
        {
            var indices = mock.GetFieldIndices(
                new ObjectValueNode(
                    new ObjectFieldNode("type", 1),
                    new ObjectFieldNode("coordinates", 2),
                    new ObjectFieldNode("crs", 26912)));
            Assert.Equal(1, indices.coordinateIndex);
        }

        private class TestGeoJsonInputType : GeoJsonInputObjectType<Point>
        {
            public override GeoJsonGeometryType GeometryType => GeoJsonGeometryType.Point;


            public new (int typeIndex, int coordinateIndex, int crsIndex) GetFieldIndices(
                ObjectValueNode obj)
            {
                return base.GetFieldIndices(obj);
            }
        }
    }
}
