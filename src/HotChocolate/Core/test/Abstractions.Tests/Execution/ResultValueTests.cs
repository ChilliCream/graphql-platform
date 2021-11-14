using Xunit;

namespace HotChocolate.Execution
{
    public class ResultValueTests
    {
        [Fact]
        public void NotInitialized()
        {
            Assert.False(default(ResultValue).IsInitialized);
        }

        [Fact]
        public void Initialized()
        {
            Assert.True(new ResultValue("abc", null).IsInitialized);
        }

        [Fact]
        public void PropertiesAreSet_NonNullable()
        {
            var value = new ResultValue("abc", "def", false);

            Assert.Equal("abc", value.Name);
            Assert.Equal("def", value.Value);
            Assert.False(value.IsNullable);
        }

        [Fact]
        public void PropertiesAreSet_Nullable()
        {
            var value = new ResultValue("abc", "def", true);

            Assert.Equal("abc", value.Name);
            Assert.Equal("def", value.Value);
            Assert.True(value.IsNullable);
        }

        [Fact]
        public void Equals_True()
        {
            // arrange
            var valueA = new ResultValue("abc", "def", true);
            var valueB = new ResultValue("abc", "def", true);

            //act
            var equal = valueA.Equals(valueB);

            // assert
            Assert.True(equal);
        }

        [Fact]
        public void Object_Equals_True()
        {
            // arrange
            var valueA = new ResultValue("abc", "def", true);
            var valueB = new ResultValue("abc", "def", true);

            //act
            var equal = Equals(valueA, valueB);

            // assert
            Assert.True(equal);
        }

        [Fact]
        public void Equals_False()
        {
            // arrange
            var valueA = new ResultValue("abc", "def", true);
            var valueB = new ResultValue("abc", "def1", true);

            //act
            var equal = valueA.Equals(valueB);

            // assert
            Assert.False(equal);
        }

        [Fact]
        public void Object_Equals_False()
        {
            // arrange
            var valueA = new ResultValue("abc", "def", true);
            var valueB = new ResultValue("abc", "def1", true);

            //act
            var equal = Equals(valueA, valueB);

            // assert
            Assert.False(equal);
        }

        [Fact]
        public void GetHashCode_Equals()
        {
            // arrange
            var valueA = new ResultValue("abc", "def", true);
            var valueB = new ResultValue("abc", "def", true);

            // act
            // assert
            Assert.Equal(valueA.GetHashCode(), valueB.GetHashCode());
        }

        [Fact]
        public void GetHashCode_Not_Equals()
        {
            // arrange
            var valueA = new ResultValue("abc", "def", true);
            var valueB = new ResultValue("abc", "def1", true);

            // act
            // assert
            Assert.NotEqual(valueA.GetHashCode(), valueB.GetHashCode());
        }
    }
}
