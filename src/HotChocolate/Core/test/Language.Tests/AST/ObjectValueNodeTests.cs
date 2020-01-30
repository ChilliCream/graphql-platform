using Xunit;

namespace HotChocolate.Language
{
    public class ObjectValueNodeTests
    {
        [Fact]
        public void GetHashCode_FieldOrder_DoesNotMatter()
        {
            // arrange
            var objecta = new ObjectValueNode(
                new ObjectFieldNode("a", 123),
                new ObjectFieldNode("b", true),
                new ObjectFieldNode("c", "foo"));

            var objectb = new ObjectValueNode(
                new ObjectFieldNode("b", true),
                new ObjectFieldNode("a", 123),
                new ObjectFieldNode("c", "foo"));

            var objectc = new ObjectValueNode(
                new ObjectFieldNode("c", "foo"),
                new ObjectFieldNode("b", true),
                new ObjectFieldNode("a", 123));

            // act
            int hasha = objecta.GetHashCode();
            int hashb = objectb.GetHashCode();
            int hashc = objectc.GetHashCode();

            // assert
            Assert.Equal(hasha, hashb);
            Assert.Equal(hashb, hashc);
        }

        [Fact]
        public void GetHashCode_Different_Objects()
        {
            // arrange
            var objecta = new ObjectValueNode(
                new ObjectFieldNode("a", 123),
                new ObjectFieldNode("b", true),
                new ObjectFieldNode("c", "foo"));

            var objectb = new ObjectValueNode(
                new ObjectFieldNode("b", true),
                new ObjectFieldNode("a", 123),
                new ObjectFieldNode("c", "abc"));

            // act
            int hasha = objecta.GetHashCode();
            int hashb = objectb.GetHashCode();

            // assert
            Assert.NotEqual(hasha, hashb);
        }

        [Fact]
        public void Equals_FieldOrder_DoesNotMatter()
        {
            // arrange
            var objecta = new ObjectValueNode(
                new ObjectFieldNode("a", 123),
                new ObjectFieldNode("b", true),
                new ObjectFieldNode("c", "foo"));

            var objectb = new ObjectValueNode(
                new ObjectFieldNode("b", true),
                new ObjectFieldNode("a", 123),
                new ObjectFieldNode("c", "foo"));

            var objectc = new ObjectValueNode(
                new ObjectFieldNode("c", "foo"),
                new ObjectFieldNode("b", true),
                new ObjectFieldNode("a", 123));

            // act
            bool resulta = objecta.Equals(objectb);
            bool resultb = objectb.Equals(objectc);

            // assert
            Assert.True(resulta);
            Assert.True(resultb);
        }

        [Fact]
        public void Equals_Different_Objects()
        {
            // arrange
            var objecta = new ObjectValueNode(
                new ObjectFieldNode("a", 123),
                new ObjectFieldNode("b", true),
                new ObjectFieldNode("c", "foo"));

            var objectb = new ObjectValueNode(
                new ObjectFieldNode("b", true),
                new ObjectFieldNode("a", 123),
                new ObjectFieldNode("c", "abc"));

            // act
            bool result = objecta.Equals(objectb);

            // assert
            Assert.False(result);
        }
    }


}
