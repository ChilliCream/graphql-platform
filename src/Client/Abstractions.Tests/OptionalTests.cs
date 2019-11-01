using Xunit;

namespace StrawberryShake
{
    public class OptionalTests
    {
        [Fact]
        public void Optional_Is_Not_Set()
        {
            // arrange
            // act
            var optional = new Optional<string>();

            // assert
            Assert.False(optional.HasValue);
            Assert.True(optional.IsEmpty);
            Assert.Null(optional.Value);
        }

        [Fact]
        public void Optional_Is_Set_To_Value()
        {
            // arrange
            // act
            Optional<string> optional = "abc";

            // assert
            Assert.True(optional.HasValue);
            Assert.False(optional.IsEmpty);
            Assert.Equal("abc", optional.Value);
        }

        [Fact]
        public void Optional_Is_Set_To_Null()
        {
            // arrange
            // act
            Optional<string> optional = null;

            // assert
            Assert.True(optional.HasValue);
            Assert.False(optional.IsEmpty);
            Assert.Null(optional.Value);
        }

        [Fact]
        public void Optional_Equals_True()
        {
            // arrange
            Optional<string> a = "abc";
            Optional<string> b = "abc";

            // act
            bool result = a.Equals(b);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void Optional_Equals_True_2()
        {
            // arrange
            Optional<string> a = "abc";
            var b = "abc";

            // act
            bool result = a.Equals(b);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void Optional_Equals_False()
        {
            // arrange
            Optional<string> a = "abc";
            Optional<string> b = "def";

            // act
            bool result = a.Equals(b);

            // assert
            Assert.False(result);
        }

        [Fact]
        public void Optional_Equals_Operator_True()
        {
            // arrange
            Optional<string> a = "abc";
            Optional<string> b = "abc";

            // act
            bool result = a == b;

            // assert
            Assert.True(result);
        }

        [Fact]
        public void Optional_Equals_Operator_True_2()
        {
            // arrange
            Optional<string> a = "abc";
            var b = "abc";

            // act
            bool result = a == b;

            // assert
            Assert.True(result);
        }

        [Fact]
        public void Optional_Equals_Operator_False()
        {
            // arrange
            Optional<string> a = "abc";
            Optional<string> b = "def";

            // act
            bool result = a == b;

            // assert
            Assert.False(result);
        }

        [Fact]
        public void Optional_Not_Equals_Operator_True()
        {
            // arrange
            Optional<string> a = "abc";
            Optional<string> b = "abc";

            // act
            bool result = a != b;

            // assert
            Assert.False(result);
        }

        [Fact]
        public void Optional_Not_Equals_Operator_True_2()
        {
            // arrange
            Optional<string> a = "abc";
            var b = "abc";

            // act
            bool result = a != b;

            // assert
            Assert.False(result);
        }

        [Fact]
        public void Optional_Not_Equals_Operator_False()
        {
            // arrange
            Optional<string> a = "abc";
            Optional<string> b = "def";

            // act
            bool result = a != b;

            // assert
            Assert.True(result);
        }
    }
}
