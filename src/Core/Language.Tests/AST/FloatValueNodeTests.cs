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
            // act
            var floatValueNode = new FloatValueNode(value);

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
            var token = new SyntaxToken(
                TokenKind.StartOfFile, 0, 0, 0, 0, null);
            Location location = new Location(new Source("{}"), token, token);

            // act
            var floatValueNode = new FloatValueNode(location, value);

            // assert
            Assert.Equal(value, floatValueNode.Value);
            Assert.Equal(NodeKind.FloatValue, floatValueNode.Kind);
            Assert.Equal(location, floatValueNode.Location);
        }

        [Fact]
        public void EqualsFloatValueNode()
        {
            // arrange
            var a = new FloatValueNode("1.0");
            var b = new FloatValueNode("1.0");
            var c = new FloatValueNode("2.0");

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
            var a = new FloatValueNode("1.0");
            var b = new FloatValueNode("1.0");
            var c = new FloatValueNode("2.0");
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
            var a = new FloatValueNode("1.0");
            var b = new FloatValueNode("1.0");
            var c = new FloatValueNode("2.0");
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
            var a = new FloatValueNode("1.0");
            var b = new FloatValueNode("1.0");
            var c = new FloatValueNode("2.0");

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
            var a = new FloatValueNode("1.0");
            var b = new FloatValueNode("2.0");

            // act
            string astring = a.ToString();
            string bstring = b.ToString();

            // assert
            Assert.Equal("1.0", astring);
            Assert.Equal("2.0", bstring);
        }

        [Fact]
        public void ClassIsSealed()
        {
            Assert.True(typeof(FloatValueNode).IsSealed);
        }
    }
}
