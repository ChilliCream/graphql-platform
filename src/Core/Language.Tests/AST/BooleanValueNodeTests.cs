using Xunit;

namespace HotChocolate.Language
{
    public class BooleanValueNodeTests
    {
        [InlineData(false)]
        [InlineData(true)]
        [Theory]
        public void CreateBooleanValue(bool value)
        {
            // act
            var booleanValueNode = new BooleanValueNode(value);

            // assert
            Assert.Equal(value, booleanValueNode.Value);
            Assert.Equal(NodeKind.BooleanValue, booleanValueNode.Kind);
            Assert.Null(booleanValueNode.Location);
        }

        [InlineData(false)]
        [InlineData(true)]
        [Theory]
        public void CreateBooleanValueWithLocation(bool value)
        {
            // arrange
            var token = new SyntaxToken(
                TokenKind.StartOfFile, 0, 0, 0, 0, null);
            Location location = new Location(new Source("{}"), token, token);

            // act
            var booleanValueNode = new BooleanValueNode(location, value);

            // assert
            Assert.Equal(value, booleanValueNode.Value);
            Assert.Equal(NodeKind.BooleanValue, booleanValueNode.Kind);
            Assert.Equal(location, booleanValueNode.Location);
        }

        [Fact]
        public void EqualsBooleanValueNode()
        {
            // arrange
            var a = new BooleanValueNode(false);
            var b = new BooleanValueNode(false);
            var c = new BooleanValueNode(true);

            // act
            bool ab_result = a.Equals(b);
            bool aa_result = a.Equals(a);
            bool ac_result = a.Equals(c);
            bool anull_result = a.Equals(default(BooleanValueNode));

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
            var a = new BooleanValueNode(false);
            var b = new BooleanValueNode(false);
            var c = new BooleanValueNode(true);
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
            var a = new BooleanValueNode(false);
            var b = new BooleanValueNode(false);
            var c = new BooleanValueNode(true);
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
            var a = new BooleanValueNode(false);
            var b = new BooleanValueNode(false);
            var c = new BooleanValueNode(true);

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
            var a = new BooleanValueNode(false);
            var b = new BooleanValueNode(true);

            // act
            string astring = a.ToString();
            string bstring = b.ToString();

            // assert
            Assert.Equal("False", astring);
            Assert.Equal("True", bstring);
        }

        [Fact]
        public void ClassIsSealed()
        {
            Assert.True(typeof(BooleanValueNode).IsSealed);
        }

        [Fact]
        public void BooleanValue_WithNewValue_NewValueIsSet()
        {
            // arrange
            var booleanValueNode = new BooleanValueNode(false);

            // act
            booleanValueNode = booleanValueNode.WithValue(true);

            // assert
            Assert.True(booleanValueNode.Value);
        }
    }
}
