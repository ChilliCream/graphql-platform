using Xunit;

namespace StrawberryShake.Abstractions
{
    public class EntityIdTests
    {
        [Fact]
        public void Equals_True()
        {
            // arrange
            var a = new EntityId("abc", "def");
            var b = new EntityId("abc", "def");

            // act
            var equals = a.Equals(b);
            var equalsOp = a == b;

            // assert
            Assert.True(equals);
            Assert.True(equalsOp);
        }

        [Fact]
        public void Equals_False()
        {
            // arrange
            var a = new EntityId("abc", "def");
            var b = new EntityId("def", "def");
            var c = new EntityId("abc", "xyz");

            // act
            var equals1 = a.Equals(b);
            var equals2 = a.Equals(c);
            var equalsOp1 = a == b;
            var equalsOp2 = a == c;

            // assert
            Assert.False(equals1);
            Assert.False(equals2);
            Assert.False(equalsOp1);
            Assert.False(equalsOp2);
        }

        [Fact]
        public void Not_Equals_True()
        {
            // arrange
            var a = new EntityId("abc", "def");
            var b = new EntityId("def", "def");
            var c = new EntityId("abc", "xyz");

            // act
            var equalsOp1 = a != b;
            var equalsOp2 = a != c;

            // assert
            Assert.True(equalsOp1);
            Assert.True(equalsOp2);
        }

        [Fact]
        public void Not_Equals_False()
        {
            // arrange
            var a = new EntityId("abc", "def");
            var b = new EntityId("abc", "def");

            // act
            var equalsOp = a != b;

            // assert
            Assert.False(equalsOp);
        }

        [Fact]
        public void GetHashCode_Equals()
        {
            // arrange
            var a = new EntityId("abc", "def");
            var b = new EntityId("abc", "def");

            // act
            var hashCodeA = a.GetHashCode();
            var hashCodeB = b.GetHashCode();

            // assert
            Assert.Equal(hashCodeA, hashCodeB);
        }

        [Fact]
        public void GetHashCode_Not_Equals()
        {
            // arrange
            var a = new EntityId("abc", "def");
            var b = new EntityId("abc1", "def");
            var c = new EntityId("abc", "def1");

            // act
            var hashCodeA = a.GetHashCode();
            var hashCodeB = b.GetHashCode();
            var hashCodeC = c.GetHashCode();

            // assert
            Assert.NotEqual(hashCodeA, hashCodeB);
            Assert.NotEqual(hashCodeA, hashCodeC);
            Assert.NotEqual(hashCodeB, hashCodeC);
        }
    }
}
