using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Types.Spatial.Tests
{
    public class ParseLiteralHelperTests
    {
        private const string _typeFieldName = "type";
        private const string _coordinatesFieldName = "coordinates";

        [Fact]
        public void Helper_Finds_Fields_In_ObjectValueNode()
        {
            var indices = ParseLiteralHelper.GetFieldIndices(new ObjectValueNode(
                new ObjectFieldNode("type", 1),
                new ObjectFieldNode("coordinates", 2)), _typeFieldName, _coordinatesFieldName);

            Assert.Equal(0, indices.typeIndex);
            Assert.Equal(1, indices.coordinateIndex);
        }

        [Fact]
        public void Helper_Finds_Partial_Fields_In_ObjectValueNode()
        {
            var indices = ParseLiteralHelper.GetFieldIndices(new ObjectValueNode(
                new ObjectFieldNode("something", 1),
                new ObjectFieldNode("other", 1),
                new ObjectFieldNode("than", 1),
                new ObjectFieldNode("it", 1),
                new ObjectFieldNode("coordinates", 2)), _typeFieldName, _coordinatesFieldName);

            Assert.Equal(-1, indices.typeIndex);
            Assert.Equal(4, indices.coordinateIndex);
        }

        [Fact]
        public void Helper_Finds_No_Fields_In_ObjectValueNode()
        {
            var indices = ParseLiteralHelper.GetFieldIndices(new ObjectValueNode(
                new ObjectFieldNode("something", 1),
                new ObjectFieldNode("other", 1),
                new ObjectFieldNode("than", 1)), _typeFieldName, _coordinatesFieldName);

            Assert.Equal(-1, indices.typeIndex);
            Assert.Equal(-1, indices.coordinateIndex);
        }

        [Fact]
        public void Helper_Finds_Parital_Ignores_Case()
        {
            var indices = ParseLiteralHelper.GetFieldIndices(new ObjectValueNode(
                new ObjectFieldNode("TyPe", 1),
                new ObjectFieldNode("COORDINATES", 2)), _typeFieldName, _coordinatesFieldName);

            Assert.Equal(0, indices.typeIndex);
            Assert.Equal(1, indices.coordinateIndex);
        }

        [Fact]
        public void Helper_Finds_Parital_Exits_Early()
        {
            var indices = ParseLiteralHelper.GetFieldIndices(new ObjectValueNode(
                new ObjectFieldNode("type", 1),
                new ObjectFieldNode("coordinates", 2),
                new ObjectFieldNode("ignored", 3)), _typeFieldName, _coordinatesFieldName);

            Assert.Equal(0, indices.typeIndex);
            Assert.Equal(1, indices.coordinateIndex);
        }
    }
}
