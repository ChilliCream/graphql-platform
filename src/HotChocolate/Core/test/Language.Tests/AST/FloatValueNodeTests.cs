using System.Text;
using Xunit;

namespace HotChocolate.Language
{
    public class FloatValueNodeTests
    {
        [InlineData("1.568")]
        [InlineData("2.0")]
        [Theory]
        public void CreateFloatValue(string value)
        {
            // arrange
            byte[] buffer = Encoding.UTF8.GetBytes(value);

            // act
            var floatValueNode = new FloatValueNode(
                buffer, FloatFormat.FixedPoint);

            // assert
            Assert.Equal(value, floatValueNode.Value);
            Assert.Equal(NodeKind.FloatValue, floatValueNode.Kind);
            Assert.Null(floatValueNode.Location);
        }

        [InlineData("1.568")]
        [InlineData("2.0")]
        [Theory]
        public void CreateFloatValueWithLocation(string value)
        {
            // arrange
            byte[] buffer = Encoding.UTF8.GetBytes(value);
            var location = new Location(0, 0, 0, 0);

            // act
            var floatValueNode = new FloatValueNode(
                location, buffer, FloatFormat.FixedPoint);

            // assert
            Assert.Equal(value, floatValueNode.Value);
            Assert.Equal(NodeKind.FloatValue, floatValueNode.Kind);
            Assert.Equal(location, floatValueNode.Location);
        }

        [InlineData("1.568", 1.568)]
        [InlineData("2.0", 2.0)]
        [Theory]
        public void ToSingle(string value, float expected)
        {
            // arrange
            byte[] buffer = Encoding.UTF8.GetBytes(value);
            var location = new Location(0, 0, 0, 0);

            // act
            var floatValueNode = new FloatValueNode(
                location, buffer, FloatFormat.FixedPoint);

            // assert
            Assert.Equal(expected, floatValueNode.ToSingle());
        }

        [InlineData("1.568", 1.568)]
        [InlineData("2.0", 2.0)]
        [Theory]
        public void ToDouble(string value, double expected)
        {
            // arrange
            byte[] buffer = Encoding.UTF8.GetBytes(value);
            var location = new Location(0, 0, 0, 0);

            // act
            var floatValueNode = new FloatValueNode(
                location, buffer, FloatFormat.FixedPoint);

            // assert
            Assert.Equal(expected, floatValueNode.ToDouble());
        }

        [InlineData("1.568", 1.568)]
        [InlineData("2.0", 2.0)]
        [Theory]
        public void ToDecimal(string value, decimal expected)
        {
            // arrange
            byte[] buffer = Encoding.UTF8.GetBytes(value);
            var location = new Location(0, 0, 0, 0);

            // act
            var floatValueNode = new FloatValueNode(
                location, buffer, FloatFormat.FixedPoint);

            // assert
            Assert.Equal(expected, floatValueNode.ToDecimal());
        }

        [Fact]
        public void EqualsFloatValueNode()
        {
            // arrange
            var a = new FloatValueNode(1.0);
            var b = new FloatValueNode(1.0);
            var c = new FloatValueNode(3.0);

            // act
            bool ab_result = a.Equals(b);
            bool aa_result = a.Equals(a);
            bool ac_result = a.Equals(c);
            bool anull_result = a.Equals(default(FloatValueNode));

            // assert
            Assert.True(ab_result);
            Assert.True(aa_result);
            Assert.False(ac_result);
            Assert.False(anull_result);
        }

        [Fact]
        public void EqualsFloatValueNode_Float()
        {
            // arrange
            var a = new FloatValueNode((float)1.0);
            var b = new FloatValueNode((float)1.0);
            var c = new FloatValueNode((float)3.0);

            // act
            bool ab_result = a.Equals(b);
            bool aa_result = a.Equals(a);
            bool ac_result = a.Equals(c);
            bool anull_result = a.Equals(default(FloatValueNode));

            // assert
            Assert.True(ab_result);
            Assert.True(aa_result);
            Assert.False(ac_result);
            Assert.False(anull_result);
        }

        [Fact]
        public void EqualsFloatValueNode_Double()
        {
            // arrange
            var a = new FloatValueNode((double)1.0);
            var b = new FloatValueNode((double)1.0);
            var c = new FloatValueNode((double)3.0);

            // act
            bool ab_result = a.Equals(b);
            bool aa_result = a.Equals(a);
            bool ac_result = a.Equals(c);
            bool anull_result = a.Equals(default(FloatValueNode));

            // assert
            Assert.True(ab_result);
            Assert.True(aa_result);
            Assert.False(ac_result);
            Assert.False(anull_result);
        }

        [Fact]
        public void EqualsFloatValueNode_Decimal()
        {
            // arrange
            var a = new FloatValueNode((decimal)1.0);
            var b = new FloatValueNode((decimal)1.0);
            var c = new FloatValueNode((decimal)3.0);

            // act
            bool ab_result = a.Equals(b);
            bool aa_result = a.Equals(a);
            bool ac_result = a.Equals(c);
            bool anull_result = a.Equals(default(FloatValueNode));

            // assert
            Assert.True(ab_result);
            Assert.True(aa_result);
            Assert.False(ac_result);
            Assert.False(anull_result);
        }

        [Fact]
        public void EqualsIValueNode()
        {
            // arrange
            var a = new FloatValueNode(1.0);
            var b = new FloatValueNode(1.0);
            var c = new FloatValueNode(2.0);
            var d = new StringValueNode("foo");

            // act
            bool ab_result = a.Equals((IValueNode)b);
            bool aa_result = a.Equals((IValueNode)a);
            bool ac_result = a.Equals((IValueNode)c);
            bool ad_result = a.Equals((IValueNode)d);
            bool anull_result = a.Equals(default(IValueNode));

            // assert
            Assert.True(ab_result);
            Assert.True(aa_result);
            Assert.False(ac_result);
            Assert.False(ad_result);
            Assert.False(anull_result);
        }

        [Fact]
        public void EqualsObject()
        {
            // arrange
            var a = new FloatValueNode(1.0);
            var b = new FloatValueNode(1.0);
            var c = new FloatValueNode(2.0);
            var d = "foo";
            var e = 1;

            // act
            bool ab_result = a.Equals((object)b);
            bool aa_result = a.Equals((object)a);
            bool ac_result = a.Equals((object)c);
            bool ad_result = a.Equals((object)d);
            bool ae_result = a.Equals((object)e);
            bool anull_result = a.Equals(default(object));

            // assert
            Assert.True(ab_result);
            Assert.True(aa_result);
            Assert.False(ac_result);
            Assert.False(ad_result);
            Assert.False(ae_result);
            Assert.False(anull_result);
        }

        [Fact]
        public void CompareGetHashCode()
        {
            // arrange
            var a = new FloatValueNode(1.0);
            var b = new FloatValueNode(1.0);
            var c = new FloatValueNode(2.0);

            // act
            int ahash = a.GetHashCode();
            int bhash = b.GetHashCode();
            int chash = c.GetHashCode();

            // assert
            Assert.Equal(ahash, bhash);
            Assert.NotEqual(ahash, chash);
        }

        [Fact]
        public void StringRepresentation()
        {
            // arrange
            var a = new FloatValueNode(1.0);
            var b = new FloatValueNode(2.0);

            // act
            string astring = a.ToString();
            string bstring = b.ToString();

            // assert
            Assert.Equal("1.00", astring);
            Assert.Equal("2.00", bstring);
        }

        [Fact]
        public void ClassIsSealed()
        {
            Assert.True(typeof(FloatValueNode).IsSealed);
        }

        [Fact]
        public void Convert_Value_Float_To_Span_To_String()
        {
            // act
            var a = new FloatValueNode(2.5);
            var b = a.WithValue(a.AsSpan(), FloatFormat.FixedPoint);
            string c = b.Value;

            // assert
            Assert.Equal("2.50", c);
        }
    }
}
