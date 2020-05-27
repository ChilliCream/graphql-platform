using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Types.Spatial.Tests
{
    public class GeoJSONPositionScalarTest
    {
        [Fact]
        public void IsInstanceOfType_Valid2ElementCoordinate_True()
        {
            // arrange
            var type = new GeoJSONPositionScalar();
            var coordinate = new ListValueNode(new IntValueNode(1), new FloatValueNode(1.2));

            // act
            bool result = type.IsInstanceOfType(coordinate);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void IsInstanceOfType_Valid3ElementCoordinate_True()
        {
            // arrange
            var type = new GeoJSONPositionScalar();
            var coordinate = new ListValueNode(
                new IntValueNode(1), new FloatValueNode(1.2), new FloatValueNode(3.2));

            // act
            bool result = type.IsInstanceOfType(coordinate);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void IsInstanceOfType_Invalid2ElementCoordinate_False()
        {
            // arrange
            var type = new GeoJSONPositionScalar();
            var coordinate = new ListValueNode(new StringValueNode("1"), new FloatValueNode(1.2));

            // act
            bool result = type.IsInstanceOfType(coordinate);

            // assert
            Assert.False(result);
        }

        [Fact]
        public void IsInstanceOfType_List2ElementCoordinate_False()
        {
            // arrange
            var type = new GeoJSONPositionScalar();
            var coordinate = new ListValueNode(
                new ListValueNode(new FloatValueNode(1.1), new FloatValueNode(1.2)));

            // act
            var result = type.IsInstanceOfType(coordinate);

            // assert
            Assert.False(result);
        }

        [Fact]
        public void IsInstanceOfType_Invalid3ElementCoordinate_False()
        {
            var type = new GeoJSONPositionScalar();
            var coordinate = new ListValueNode(
                new IntValueNode(1),
                new IntValueNode(2),
                new IntValueNode(3),
                new IntValueNode(4)
            );

            bool result = type.IsInstanceOfType(coordinate);

            Assert.False(result);
        }
    }
}
